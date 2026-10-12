import { get, post, put, del } from "../api.js";
import { h, toast, timeAgo } from "../dom.js";
import { dropIndex, moveCard, localToday, dueState, dueLabel, matches } from "../boardModel.js";
import { openCardDialog } from "../cardDialog.js";

export async function render(view, ctx) {
  const boardId = Number(ctx.params[0]);
  let board = await get(`/boards/${boardId}`);

  let filterText = "";
  let filterPriority = "";
  let activityOpen = false;
  let drag = null;               // { cardId } while a card is being dragged
  let refocusColumn = null;      // after adding a card, the box stays focused for the next one
  let saving = Promise.resolve(); // moves are sent one after the other, in the order they happened

  const columnsBox = h("div", { class: "columns" });
  const activityBox = h("aside", { class: "activity", "aria-label": "Recent activity", hidden: true });
  const title = h("h1", {}, board.name);

  // ---------- talking to the server ----------

  async function reload() {
    board = await get(`/boards/${boardId}`);
    title.textContent = board.name;
    draw();
  }

  function move(cardId, columnId, index) {
    const before = board.columns.flatMap((c) => c.cards).find((c) => c.id === cardId)?.columnId;
    if (!moveCard(board, cardId, columnId, index)) { draw(); return; }
    draw();

    // the screen is already updated; this call makes it permanent
    saving = saving.then(async () => {
      try {
        await post(`/cards/${cardId}/move`, { columnId, index });
        if (activityOpen && before !== columnId) await loadActivity();
      } catch (err) {
        toast(err.message, "error");
        await reload(); // show what the server really has
      }
    });
  }

  async function guarded(action) {
    try { await action(); } catch (err) { toast(err.message, "error"); }
  }

  // ---------- drag and drop ----------

  function cardItems(list) {
    return [...list.children].filter((el) => el.classList.contains("card") && !el.classList.contains("dragging"));
  }

  function indexFor(list, mouseY) {
    const middles = cardItems(list).map((el) => {
      const r = el.getBoundingClientRect();
      return r.top + r.height / 2;
    });
    return dropIndex(middles, mouseY);
  }

  function showMarker(list, index) {
    list.querySelector(".drop-marker")?.remove();
    const marker = h("li", { class: "drop-marker", "aria-hidden": "true" });
    list.insertBefore(marker, cardItems(list)[index] ?? null);
  }

  function clearMarkers() {
    columnsBox.querySelectorAll(".drop-marker").forEach((m) => m.remove());
    columnsBox.querySelectorAll(".over").forEach((m) => m.classList.remove("over"));
  }

  function wireDropZone(list, column) {
    list.addEventListener("dragover", (e) => {
      if (!drag) return;
      e.preventDefault(); // without this the browser refuses the drop
      if (e.dataTransfer) e.dataTransfer.dropEffect = "move";
      list.classList.add("over");
      showMarker(list, indexFor(list, e.clientY));
    });
    list.addEventListener("dragleave", (e) => {
      if (!list.contains(e.relatedTarget)) {
        list.classList.remove("over");
        list.querySelector(".drop-marker")?.remove();
      }
    });
    list.addEventListener("drop", (e) => {
      if (!drag) return;
      e.preventDefault();
      const index = indexFor(list, e.clientY);
      const cardId = drag.cardId;
      drag = null;
      clearMarkers();
      move(cardId, column.id, index);
    });
  }

  // ---------- drawing ----------

  const filtering = () => Boolean(filterText.trim() || filterPriority);

  function cardEl(column, card) {
    const today = localToday();
    const state = dueState(card, column, today);
    const at = board.columns.indexOf(column);
    const left = board.columns[at - 1];
    const right = board.columns[at + 1];

    const li = h("li", {
      class: `card prio-${card.priority.toLowerCase()}`,
      draggable: filtering() ? null : "true",
      "data-card": card.id,
      ondragstart: (e) => {
        drag = { cardId: card.id };
        e.dataTransfer.setData("text/plain", String(card.id)); // Firefox needs some data to start a drag
        e.dataTransfer.effectAllowed = "move";
        setTimeout(() => li.classList.add("dragging"), 0);
      },
      ondragend: () => {
        drag = null;
        li.classList.remove("dragging");
        clearMarkers();
      },
    },
      h("button", { class: "card-title", type: "button", onclick: () => edit(card) }, card.title),
      h("div", { class: "card-meta" },
        card.priority !== "Normal" ? h("span", { class: `chip chip-${card.priority.toLowerCase()}` }, card.priority) : null,
        state !== "none" ? h("span", { class: `chip chip-due-${state}` }, dueLabel(card, today)) : null,
        card.description ? h("span", { class: "chip chip-note", title: "Has notes" }, "Notes") : null),
      // the same move as dragging, for people who can't or don't want to drag
      h("div", { class: "card-move" },
        left ? h("button", { class: "mini", type: "button", "aria-label": `Move "${card.title}" to ${left.name}`, onclick: () => move(card.id, left.id, left.cards.length) }, "←") : null,
        right ? h("button", { class: "mini", type: "button", "aria-label": `Move "${card.title}" to ${right.name}`, onclick: () => move(card.id, right.id, right.cards.length) }, "→") : null));
    return li;
  }

  function columnEl(column) {
    const visible = column.cards.filter((c) => matches(c, filterText, filterPriority));
    const list = h("ul", { class: "cards", "data-column": column.id, "aria-label": `${column.name} cards` },
      visible.map((c) => cardEl(column, c)));
    if (!filtering()) wireDropZone(list, column);

    const input = h("input", { type: "text", maxlength: 120, placeholder: "Add a card", "aria-label": `New card in ${column.name}` });
    const addForm = h("form", {
      class: "add-card",
      onsubmit: (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text) return;
        guarded(async () => {
          const card = await post(`/columns/${column.id}/cards`, { title: text, description: null, priority: "Normal", dueDate: null });
          column.cards.push(card);
          refocusColumn = column.id;
          draw();
        });
      },
    }, input);
    input.dataset.addTo = column.id;

    return h("section", { class: "column" + (column.isDone ? " column-done" : ""), "aria-label": column.name },
      h("header", { class: "column-head" },
        h("h2", {}, column.name, " ", h("span", { class: "count" }, String(column.cards.length))),
        h("div", { class: "column-tools" },
          h("button", { class: "mini", type: "button", onclick: () => renameColumn(column) }, "Rename"),
          h("button", { class: "mini", type: "button", onclick: () => removeColumn(column) }, "Delete"))),
      list,
      addForm);
  }

  function addColumnEl() {
    const name = h("input", { type: "text", maxlength: 40, placeholder: "New column", "aria-label": "New column name" });
    const done = h("input", { type: "checkbox" });
    return h("form", {
      class: "column column-new",
      onsubmit: (e) => {
        e.preventDefault();
        const text = name.value.trim();
        if (!text) return;
        guarded(async () => {
          const column = await post(`/boards/${boardId}/columns`, { name: text, isDone: done.checked });
          board.columns.push(column);
          draw();
        });
      },
    },
      name,
      h("label", { class: "check" }, done, " Counts as done"),
      h("button", { class: "btn btn-small", type: "submit" }, "Add column"));
  }

  function draw() {
    columnsBox.replaceChildren(...board.columns.map(columnEl), addColumnEl());
    if (refocusColumn !== null) {
      columnsBox.querySelector(`[data-add-to="${refocusColumn}"]`)?.focus();
      refocusColumn = null;
    }
  }

  // ---------- actions ----------

  function edit(card) {
    openCardDialog({
      card,
      columns: board.columns,
      onSave: async (values) => {
        const { columnId, ...rest } = values;
        const saved = await put(`/cards/${card.id}`, rest);
        Object.assign(card, saved);
        if (columnId !== card.columnId) {
          const target = board.columns.find((c) => c.id === columnId);
          move(card.id, columnId, target.cards.length);
        } else {
          draw();
        }
      },
      onDelete: async () => {
        await del(`/cards/${card.id}`);
        for (const c of board.columns) c.cards = c.cards.filter((k) => k.id !== card.id);
        draw();
      },
    });
  }

  function renameColumn(column) {
    const name = window.prompt("Column name", column.name)?.trim();
    if (!name || name === column.name) return;
    guarded(async () => {
      await put(`/columns/${column.id}`, { name, isDone: column.isDone });
      column.name = name;
      draw();
    });
  }

  function removeColumn(column) {
    if (!window.confirm(`Delete the column "${column.name}"?`)) return;
    guarded(async () => {
      await del(`/columns/${column.id}`); // the server says no if it still has cards
      board.columns = board.columns.filter((c) => c.id !== column.id);
      draw();
    });
  }

  function renameBoard() {
    const name = window.prompt("Board name", board.name)?.trim();
    if (!name || name === board.name) return;
    guarded(async () => {
      await put(`/boards/${boardId}`, { name });
      board.name = name;
      title.textContent = name;
    });
  }

  function removeBoard() {
    if (!window.confirm(`Delete the board "${board.name}" with all its cards? This can't be undone.`)) return;
    guarded(async () => {
      await del(`/boards/${boardId}`);
      toast("Board deleted.");
      location.hash = "#/boards";
    });
  }

  async function loadActivity() {
    const items = await get(`/boards/${boardId}/activity`);
    activityBox.replaceChildren(
      h("h2", {}, "Recent activity"),
      items.length
        ? h("ul", { class: "plain" }, items.map((a) => h("li", {}, h("span", {}, a.text), h("small", { class: "muted" }, timeAgo(a.atUtc)))))
        : h("p", { class: "muted" }, "Nothing has been moved yet."));
  }

  // ---------- toolbar ----------

  const search = h("input", {
    type: "search", placeholder: "Filter cards", "aria-label": "Filter cards", maxlength: 60,
    oninput: () => { filterText = search.value; draw(); updateHint(); },
  });
  const priority = h("select", {
    "aria-label": "Filter by priority",
    onchange: () => { filterPriority = priority.value; draw(); updateHint(); },
  },
    h("option", { value: "" }, "Any priority"),
    ["High", "Normal", "Low"].map((p) => h("option", { value: p }, p)));
  const hint = h("p", { class: "muted hint", hidden: true }, "Dragging is off while a filter is on, because the positions you see are not the real ones.");
  const updateHint = () => { hint.hidden = !filtering(); };

  const activityButton = h("button", {
    class: "btn btn-small btn-ghost", type: "button", "aria-expanded": "false",
    onclick: () => guarded(async () => {
      activityOpen = !activityOpen;
      activityBox.hidden = !activityOpen;
      activityButton.setAttribute("aria-expanded", String(activityOpen));
      if (activityOpen) await loadActivity();
    }),
  }, "Activity");

  view.replaceChildren(
    h("p", {}, h("a", { href: "#/boards" }, "All boards")),
    h("div", { class: "board-head" },
      title,
      h("div", { class: "actions" },
        h("button", { class: "btn btn-small btn-ghost", type: "button", onclick: renameBoard }, "Rename"),
        h("button", { class: "btn btn-small btn-danger", type: "button", onclick: removeBoard }, "Delete board"))),
    h("div", { class: "toolbar" }, search, priority, activityButton),
    hint,
    h("div", { class: "board-body" }, columnsBox, activityBox));

  draw();
}
