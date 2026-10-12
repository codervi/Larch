# Larch

A small kanban board. You make boards with columns, put cards in them, and drag the cards around. Everything is saved, so the board looks the same when you come back.

I wrote it to practise drag and drop **without a library**, and to keep the order of cards correct in the database.

Back end is an ASP.NET Core Web API (.NET 8) with EF Core and SQL Server. The front end is plain HTML, CSS and JavaScript (ES modules, no framework, no drag-and-drop library) in `wwwroot`.

## What it does

- Accounts (register, sign in), every user sees only their own boards
- Boards start with To do, Doing and Done. Add, rename and delete columns (a column must be empty before it can be deleted)
- Cards with title, notes, priority (Low, Normal, High) and an optional due date
- Drag a card to another column or to another place in the same column. A line shows where it will land.
- The same move works without dragging: every card has arrow buttons for the neighbouring columns
- Due dates: "Due today", "3 days late". Cards in a column marked as done are never shown as late.
- Filter by text or priority. Dragging is switched off while a filter is on, because the positions you see are then not the real ones.
- Activity list: "Sam moved 'Build the contact form' from To do to Doing"

## How the order is kept

Each card has a `Position` (0, 1, 2 ...) inside its column. When a card is dropped:

1. The browser works out the index from the mouse height and the middle of each card (`dropIndex` in `js/boardModel.js`), redraws the board straight away, and sends `POST /api/cards/{id}/move` with the column and the index.
2. The server (`CardOrdering.Place`) takes the ids of the cards in the target column, puts the moving card at that index and numbers the column again from 0. If the card changed column, the column it left is renumbered too, so there are never gaps or duplicates.
3. If the request fails, the board is loaded again from the server so the screen never keeps a wrong order.

Moves are sent one after another, in the order they were made, so two quick drags can't overtake each other.

There is no UNIQUE index on `(ColumnId, Position)` on purpose: while the numbers are rewritten two rows can briefly have the same value. There is a normal index on it for the "cards of this column, in order" query.

## Other things worth a look

- The board page is one query that returns columns and cards already ordered (`BoardsController.Get`). The board list counts cards and late cards in SQL without loading them.
- Every query starts from a board that belongs to the signed-in user (`OwnedController`). Another user's id gives 404, not 403, so ids can't be probed. A card can only be moved to a column of its own board.
- Deleting a board is one `ExecuteDeleteAsync`; columns, cards and history are removed by cascade delete in the database.
- Login is an Identity cookie (HttpOnly, Secure, SameSite=Strict). Changes need an `X-Requested-With` header on top of that. Login and register are rate limited and the account locks after 5 wrong passwords.
- The page is built with `createElement` and text nodes; card text never goes through `innerHTML`.

## Running it

You need the .NET 8 SDK and SQL Server or LocalDB.

```
cd src/Larch.Api
dotnet dev-certs https --trust
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet run
```

The database is created on startup. Open https://localhost:7230 (the API docs are at /swagger).

In Development one account is added, with two boards: `sam@larch.test`, password `Demo#2024pass`.

## Tests

```
dotnet test
```

The tests cover the ordering rules (top, middle, end, empty column, index out of range, moving inside one column). The database queries and the controllers have no tests.

## Layout

```
src/Larch.Api
  Controllers/   Auth, Boards, Columns (and cards in a column), Cards (edit, delete, move)
  Services/      CardOrdering
  Models/        Board, Column, Card, Activity
  Data/          DbContext, seed
  wwwroot/       index.html, css, js (app.js router, api.js, boardModel.js, cardDialog.js, views/)
tests/Larch.Tests
```

## Left out on purpose

Sharing a board with other users, reordering columns, file attachments, real-time updates between two browsers.
