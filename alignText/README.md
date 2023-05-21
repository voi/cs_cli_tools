---
date: 2023-05-21
---

# Align Text CLI

## build

~~~
set PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319;%PATH%
csc.exe alignText.cs
~~~


## usage

input/output clipboard.
~~~
alignText.exe -c [word to alitn]
~~~

input/output pipe.
~~~
(input) | alignText.exe -p [word to align] > (output)
~~~

| option | desc
| ---- | ---
| `-c` | use clipboard to input/output.
| `-p` | use stdin/stdout to input/output
| `-g` | align all target word (default: align first target word only)
| `-t` | use tab (default: use space)
| `-a` | align after target word (default: align before target word)
| `-r` | target word is as regex (default: target word is as literal text)

