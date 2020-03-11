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

        internal const string TruancyInfoQuery = @"
select trim(stfname)   as first_name,
       trim(stlname)   as last_name,
       trim(stgrade)   as grade,
       concat(trim(sthphone), trim(sthphone1)) as phone,
       trim(stdistrict) as district,
       concat(trim(staddress1911), '  ', trim(staddress2911)) as address,
       trim(stcity911) as city,
       trim(ststate911) as state,
       trim(stzip911)  as zip,
       concat(trim(stmfname), ' ', trim(stmlname)) as mother_name,
       concat(trim(stffname), ' ', trim(stflname)) as father_name,
       trim(stmemail) as mother_email,
       trim(stfemail) as father_email,
       concat(trim(stmcellph), trim(stmcellph1)) as mother_cell,
       concat(trim(stfcellph), trim(stfcellph1)) as father_cell
from stu0001
where styear = @y and stsidno = @s;";

        internal const string TruancyEntryDateQuery = @"
select trim(stsidno) as sid
from stu0001
where styear = @y and stschool = '001' and stedate < current_date - interval '14d'";
    }
}
