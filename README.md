# Words
For the Xbox game "guess that word" 

## Current framework
- Word length selection: 4-20 letters, default 5
- Concurrent words: 1-100, default 1
- Guess budget: 6 base guesses plus 1 per extra word
- In-memory leaderboard via `ScoreService`
- PvP match system with head-to-head race-to-N rounds and open-ended play
