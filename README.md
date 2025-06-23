# MonkeyBoard 

<p style="text-align: center;"><img style="width: 25%" src="ss/ss1.jpg" />    <img style="width: 25%" src="ss/ss2.jpg" />        <img style="width: 25%" src="ss/ss5.jpg" /><img style="width: 25%" src="ss/ss3.jpg" />    <img style="width: 25%" src="ss/ss4.jpg" /></p>


Softkeyboard compatible with **both** Android and iOS.

Currently supports (with varying degrees of functionality) **53 different cultures** all with unique sets of keyboards and language dictionaries.

## Features
- Autocomplete, autocorrect, word learning and word forgetting (also emojis)
- Spacebar cursor controls
- Emoji mode with emoji search and recently used list
- Floating mode
- Mobile and tablet layouts and UX
- Dark/light themes
- Smart punctuation, auto-capitalization
- And much more...

## Building from source
1. Language packs are NOT included in this repo but all are available on my website [here](https://www.monkeypaste.com/dat/kb/kb-index.json). 
2. You'll need to move them to this relative path
`src/MonkeyBoard.Common/Assets/Localization/packs/<culture-code>/<culture-code>.zip`
3. (if not using en-US) Update `src/MonkeyBoard.Common/MonkeyBoard.Common.csproj` with the path from Step #2

## Special Thanks
[AnySoftKeyboard](https://github.com/anysoftkeyboard)




