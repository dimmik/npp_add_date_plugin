# npp_add_date_plugin
On each newline ({(AddDateKey == '\r' ? "CR" : "LF")}) in files with extension {ApplicableExtension} adds a date in format {DatetimeFmt} at the line start
Char {ToggleAddDateChar} ('{ToggleAddDateChar}') toggles adding date.
On startup 'add date' is enabled
Config file: '{Path.GetFullPath(iniFilePath)}'
Is Date currently being added? {DoInsertDate}
Is currently enabled? {Enabled}

Example of config:
~~~
ApplicableExtension=.wlog
DateTimeFmt= yyyy.MM.dd HH:mm:ss
ToggleAddDateChar=~
Enabled=true
AddDateKey=\n
~~~