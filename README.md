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
- Xbox achievements unlock for first wins, score milestones, and rank milestones
