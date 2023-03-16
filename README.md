# npp_add_date_plugin
On each newline (CR or LF, depending on config. Default **LF**) in files with extension **.wlog** (can be changed in config) adds a date in format **yyyy.MM.dd HH:mm:ss** (also configurable) at the line start

Char **~** ('~') toggles adding date. Configurable.

On startup adding date is toggled on.

Can be disabled in config.

Example of config:
~~~
ApplicableExtension=.wlog
DateTimeFmt= yyyy.MM.dd HH:mm:ss
ToggleAddDateChar=~
Enabled=true
AddDateKey=\n
~~~