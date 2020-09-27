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
       concat(trim(stgfname), ' ', trim(stglname)) as guardian_name,
       trim(stmemail) as mother_email,
       trim(stfemail) as father_email,
       trim(stgemail) as guardian_email,
       concat(trim(stmcellph), trim(stmcellph1)) as mother_cell,
       concat(trim(stfcellph), trim(stfcellph1)) as father_cell,
       concat(trim(stgcellph), trim(stgcellph1)) as guardian_cell,
       stbirthdate as dob,
       stedate as entry_date,
       trim(stgender) as gender,
       trim(stschool) as school,
       trim(stresdistrict) as residence_district_code,
       trim(cddescription) as residence_district_name
from stu0001
inner join code on stresdistrict = cdcode and cdtype = 'districts'
where styear = @y and stsidno = @s;";

        internal const string TruancyEntryDateQuery = @"
select trim(stsidno) as sid
from stu0001
where styear = @y 
  and stschool in ('001', '201')
  and stedate < current_date - interval '21d' 
  and stldate is null";
    }
}
