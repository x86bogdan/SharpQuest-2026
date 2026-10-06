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

1. On the course template page (<https://github.com/x86bogdan/sharpquest-2026>), click **Use this template → Create a new repository**. Not *Fork*: a fork of a public repository is public.
2. Owner: you. Name: anything, e.g. `fcpl-lab`. Visibility: **Private**.
3. In your new repository: **Settings → Collaborators → Add people** → `x86bogdan`.
4. Open `README.md`, click the pencil, fill in the section above, **Commit changes**.
5. Paste your repository's URL into the claim form, along with your avatar.

That's all for session 1. Cloning and pushing start in session 2.

## Every lab, the same loop

```
sq update      # at the start of the lab: fetch new contracts, reference solutions and catch-up kits
sq test        # as often as you like: build + run everything, see what's left
git push       # when it's green: CI runs the same tests and records your result
```

On Windows, run `sq` from the repository folder (in PowerShell: `.\sq test`). On Linux or macOS: `sh sq.sh test`.

**No git, on a lab PC?** Download this repository as a ZIP (**Code → Download ZIP**), extract it, open the folder in Rider and use Rider's terminal: `sh sq.sh update`, `sh sq.sh test`. To save your work, `sh sq.sh upload` gathers what to upload, and **Add file → Upload files** on this page takes it. Step by step: [working on a lab PC](https://github.com/x86bogdan/fcpl-2026/blob/main/LAB-PC.md).
`sq` is a small C# program (`sq.cs`), built and run by `dotnet run --project tools/sq`. The first run takes 10–20 seconds while it builds itself. You can read it — session 12 explains how it works.

**Labs you haven't started show as "not started", not as failures.** Contracts can appear before their session. A lab starts when you add its wiring class (see `src/Capstone.Core/Wiring/`).

**Missed a lab, or didn't finish it?** After each session, `sq update` brings one of two things for that lab:

- **A catch-up kit, in `catchup/LabNN/`**, for the labs that later labs build on (02, 05, 06 and 08). It's the smallest working piece that lets you carry on, written for no particular theme. Copy its files into `src/Capstone.Core/CatchUp/` and next week's lab works. **It earns nothing for the lab you missed:** the tests ignore it, so that lab stays *not started*, and a wiring class of your own that hands back the kit's objects fails by name. To get the points, write your own version for your own theme, stop using the kit, and recover the lab in session 13 or 14. `catchup/README.md` has the details.
- **A reference solution, in `reference/LabNN/`**, for every other lab: one theme's solution. If it's your theme you may copy it (say so in this README). Otherwise read it for the shape and write your own.

Late work and recovery (each worth up to 8; at most 2 recoveries per recovery session, 4 in total) are explained in the [course contract](https://github.com/x86bogdan/fcpl-2026/blob/main/CONTRACT.md).

## What's in here

| Path | What it is | Yours? |
|---|---|---|
| `src/Capstone.Core/` | Your domain: types, logic, storage. The tests look here. | ✅ |
| `src/Capstone.Core/Wiring/` | One small class per lab that tells the tests how to create your objects | ✅ |
| `src/Capstone.App/` | The program you run. Keep it thin. | ✅ |
| `tests/Capstone.Tests/` | Your own tests (from session 3) | ✅ |
| `Directory.Packages.local.props` | Extra NuGet packages you want (create it if you need it) | ✅ |
| `contracts/` | Each lab's interfaces and the tests that grade them | 🔒 read, don't edit |
| `reference/` | One theme's solution for each finished lab that isn't a kit lab | 🔒 read, don't edit |
| `catchup/` | Catch-up kits for Labs 02, 05, 06 and 08, once published | 🔒 copy from, don't edit |
| `Directory.Build.*`, `Directory.Packages.props`, `global.json` | Shared build settings | 🔒 |
| `sq.cs`, `sq.cmd`, `sq.sh`, `tools/sq/`, `.config/dotnet-tools.json`, `.github/workflows/ci.yml` | Tooling | 🔒 |

🔒 files are replaced with the official copies by `sq update`, and again by CI before it grades. Editing them changes nothing that counts. It only makes your local results disagree with the recorded ones.

## How this is graded

Each lab is worth 10 points: **Contract 40%**, **Change 30%**, **Comprehension 30%**.

- **Contract** comes from the contract tests. The number that counts is the one CI records when you push, not the one on your screen.
- **Change** and **Comprehension** happen in the room: a small live modification to your code, and two "why" questions about it. AI and tutorials are allowed if you say so. What's graded is whether you can work with the code you submitted.

The full rules are in the [course contract](https://github.com/x86bogdan/fcpl-2026/blob/main/CONTRACT.md).

## Rules the build enforces

- **Zero nullable warnings.** In this repository, a nullable warning is a build error. `#nullable disable` compiles, but it gets flagged to your instructor.
- **.NET 9 and C# 13, on every machine.** The lab PCs have .NET 9; a .NET 10 SDK builds the same thing. Newer C# features are rejected by the build even on a .NET 10 laptop, so what builds on yours also builds on a lab PC and on GitHub. Details: [.NET 9 or .NET 10](https://github.com/x86bogdan/fcpl-2026/blob/main/DOTNET-9.md).
