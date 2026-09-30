# My FCPL lab capstone

*The FCPL lab runs as **SharpQuest**, the quest to learn C#. That's why the tooling and some code say SharpQuest.*

> **Edit this section in session 1.** It's your first commit.
>
> **Theme:** _which of the catalogue themes you picked_
> **In one sentence:** _what your app will do by January_
> **Route:** _guided or project_
>
> Don't write your name or your avatar here. The scoreboard is anonymous, and this file is how people would find out.

---

## Getting your own copy (session 1, browser only)

1. On the course template page, click **Use this template → Create a new repository**.
2. Owner: you. Name: anything, e.g. `fcpl-lab`. Visibility: **Private**.
3. In your new repository: **Settings → Collaborators → Add people** → your instructor's GitHub username.
4. Open `README.md`, click the pencil, fill in the section above, **Commit changes**.
5. Paste your repository's URL into the claim form, along with your avatar.

That's all for session 1. Cloning and pushing start in session 2.

## Every lab, the same loop

```
sq update      # at the start of the lab: fetch new contracts and reference solutions
sq test        # as often as you like: build + run everything, see what's left
git push       # when it's green: CI runs the same tests and records your result
```

On Windows, run `sq` from the repository folder (in PowerShell: `.\sq test`). On Linux or macOS: `./sq.sh test`.
`sq` is a small C# program (`sq.cs`), run by `dotnet run --file sq.cs`. You can read it — session 12 explains how it works.

**Labs you haven't started show as "not started", not as failures.** Contracts can appear before their session. A lab starts when you add its wiring class (see `src/Capstone.Core/Wiring/`).

**Missed a lab?** `reference/LabNN/` has one theme's solution, published after the session. If it's your theme you may copy it (say so). Otherwise read it for the shape and write your own. Recovery rules are in the course contract.

## What's in here

| Path | What it is | Yours? |
|---|---|---|
| `src/Capstone.Core/` | Your domain: types, logic, storage. The tests look here. | ✅ |
| `src/Capstone.Core/Wiring/` | One small class per lab that tells the tests how to create your objects | ✅ |
| `src/Capstone.App/` | The program you run. Keep it thin. | ✅ |
| `tests/Capstone.Tests/` | Your own tests (from session 3) | ✅ |
| `Directory.Packages.local.props` | Extra NuGet packages you want (create it if you need it) | ✅ |
| `contracts/` | Each lab's interfaces and the tests that grade them | 🔒 read, don't edit |
| `reference/` | One theme's solution for each finished lab | 🔒 read, don't edit |
| `Directory.Build.*`, `Directory.Packages.props`, `global.json` | Shared build settings | 🔒 |
| `sq.cs`, `sq.cmd`, `sq.sh`, `.github/workflows/ci.yml` | Tooling | 🔒 |

🔒 files are replaced with the official copies by `sq update`, and again by CI before it grades. Editing them changes nothing that counts. It only makes your local results disagree with the recorded ones.

## How this is graded

Each lab is worth 10 points: **Contract 40%**, **Change 30%**, **Comprehension 30%**.

- **Contract** comes from the contract tests. The number that counts is the one CI records when you push, not the one on your screen.
- **Change** and **Comprehension** happen in the room: a small live modification to your code, and two "why" questions about it. AI and tutorials are allowed if you say so. What's graded is whether you can work with the code you submitted.

The full rules are in the course contract.

## Rules the build enforces

- **Zero nullable warnings.** In this repository, a nullable warning is a build error. `#nullable disable` compiles, but it gets flagged to your instructor.
- **.NET 10.** `global.json` pins the SDK so every machine in the room builds the same way.
