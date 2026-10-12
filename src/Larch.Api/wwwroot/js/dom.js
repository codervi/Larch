// Small helpers for building the page. Text always goes in as text nodes, never as HTML.

export function h(tag, attrs, ...children) {
  const el = document.createElement(tag);
  let value;
  for (const [key, val] of Object.entries(attrs ?? {})) {
    if (val === null || val === undefined || val === false) continue;
    if (key === "class") el.className = val;
    else if (key === "value") value = val;
    else if (key.startsWith("on") && typeof val === "function") el.addEventListener(key.slice(2), val);
    else el.setAttribute(key, val === true ? "" : String(val));
  }
  for (const child of children.flat(Infinity)) {
    if (child === null || child === undefined || child === false) continue;
    el.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  if (value !== undefined) el.value = value;
  return el;
}

let nextId = 0;
export function field(label, input) {
  const id = "f" + ++nextId;
  input.id = id;
  return h("div", { class: "field" }, h("label", { for: id }, label), input);
}

export function toast(message, kind = "ok") {
  const box = document.getElementById("toasts");
  if (!box) return;
  const el = h("div", { class: "toast toast-" + kind }, message);
  box.append(el);
  setTimeout(() => el.remove(), 4000);
}

export function timeAgo(utc) {
  const then = new Date(/(Z|[+-]\d\d:\d\d)$/.test(utc) ? utc : utc + "Z").getTime();
  const minutes = Math.max(0, Math.round((Date.now() - then) / 60000));
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} h ago`;
  return `${Math.round(hours / 24)} d ago`;
}
