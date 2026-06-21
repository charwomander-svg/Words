# Words
For the Xbox game "guess that word" 

## Current framework
- Word length selection: 4-20 letters, default 5
- Concurrent words: 1-100, default 1
- Guess budget: 6 base guesses plus 1 per extra word
- In-memory leaderboard via `ScoreService`
- PvP match system with head-to-head race-to-N rounds and open-ended play
- In-round controls: QWERTY navigation with `LEFT/RIGHT`, `A` confirm, `B` delete, `X` submit, `Y` hint, `?` help, `Q` quit round
- Hint requests are capped at 3 per word and disabled in PvP
- Player progression tracks XP, rank, and cosmetic unlocks
- Xbox achievements unlock for 5-letter words starting with A-Z plus title update events
- The Xbox front end now has a stylized title screen, gameplay HUD, and shared peripheral-page template for credits/tutorial/privacy/legal screens
- Title/menu content is kept separate from core word logic so the presentation layer can evolve without changing gameplay rules
- Drop album tracks into `src/Words.Xbox/Audio/Music/` as `title.*`, `round.*`, or `credits.*`
- Drop sound effects into `src/Words.Xbox/Audio/Sfx/` as `correct.*`, `incorrect.*`, `hint.*`, `achievement.*`, `navigate.*`, `confirm.*`, `delete.*`, `round-start.*`, or `round-end.*`
