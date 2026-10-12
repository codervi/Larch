// Pure functions for the board: no DOM in here, so they are easy to test.
// A board looks like { id, name, columns: [{ id, name, isDone, cards: [{ id, title, priority, dueDate, ... }] }] }

// Where in a column a dragged card lands, given the vertical middle of every card that is
// already there (not counting the dragged one) and the mouse height.
export function dropIndex(middles, mouseY) {
  let index = 0;
  for (const middle of middles) {
    if (mouseY > middle) index++;
    else break;
  }
  return index;
}

// Moves a card inside the local copy of the board. It does the same thing as
// CardOrdering.Place on the server, so the screen and the database agree.
// Returns true when something changed.
export function moveCard(board, cardId, columnId, index) {
  let card = null;
  let fromColumn = null;
  let fromIndex = -1;

  for (const column of board.columns) {
    const i = column.cards.findIndex((c) => c.id === cardId);
    if (i !== -1) { card = column.cards[i]; fromColumn = column; fromIndex = i; break; }
  }
  const target = board.columns.find((c) => c.id === columnId);
  if (!card || !target) return false;

  fromColumn.cards.splice(fromIndex, 1);
  const slot = Math.max(0, Math.min(index, target.cards.length));
  target.cards.splice(slot, 0, card);
  card.columnId = target.id;

  return !(fromColumn === target && fromIndex === slot);
}

// "2026-10-13" for the user's own calendar day. Due dates have no time zone, so they are compared as plain text.
export function localToday(now = new Date()) {
  const pad = (n) => String(n).padStart(2, "0");
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

const dayNumber = (iso) => {
  const [y, m, d] = iso.split("-").map(Number);
  return Math.round(Date.UTC(y, m - 1, d) / 86400000);
};

// "none" (no date, or the card is in a done column), "overdue", "soon" (today or the next 2 days), "later"
export function dueState(card, column, today) {
  if (!card.dueDate || column.isDone) return "none";
  const days = dayNumber(card.dueDate) - dayNumber(today);
  if (days < 0) return "overdue";
  return days <= 2 ? "soon" : "later";
}

export function dueLabel(card, today) {
  const days = dayNumber(card.dueDate) - dayNumber(today);
  if (days === 0) return "Due today";
  if (days === 1) return "Due tomorrow";
  if (days === -1) return "1 day late";
  if (days < 0) return `${-days} days late`;
  const [y, m, d] = card.dueDate.split("-").map(Number);
  return "Due " + new Date(y, m - 1, d).toLocaleDateString("en-GB", { day: "numeric", month: "short" });
}

export function matches(card, text, priority) {
  if (priority && card.priority !== priority) return false;
  const needle = text.trim().toLowerCase();
  if (!needle) return true;
  return card.title.toLowerCase().includes(needle) || (card.description ?? "").toLowerCase().includes(needle);
}
