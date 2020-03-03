namespace CanvasReportGen {
    internal static class DatabaseStrings {
        internal const string StudentInfoQuery = @"
select concat(trim(staddress1911), '  ', trim(staddress2911)) as address,
       trim(stcity911) as city,
       trim(stzip911)  as zip,
       trim(stfname)   as first_name,
       trim(stlname)   as last_name,
       trim(stgrade)   as grade
from stu0001
where styear = @y and stsidno = @s;";
    }
}
