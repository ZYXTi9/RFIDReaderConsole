SELECT * FROM reader_tbl;
SELECT * FROM reader_settings_tbl;
SELECT * FROM antenna_tbl;
SELECT * FROM antenna_info_tbl;
SELECT * FROM gpi_tbl;
SELECT * FROM gpo_tbl;
SELECT * FROM read_tbl;
SELECT * FROM tag_storage_tbl;
SELECT * FROM power_radio_tbl;
SELECT * FROM rf_modes_tbl;
SELECT * FROM singulation_tbl;

CALL truncate_all_tables();
SELECT * FROM reader_tbl r INNER JOIN antenna_tbl a ON r.ReaderID = a.ReaderID INNER JOIN singulation_tbl s ON a.AntennaID = s.AntennaID WHERE a.ReaderID = 1  AND a.Antenna = 1;
SELECT * FROM gpo_tbl WHERE ReaderID =  1 ORDER BY GPOPort ASC;
SELECT * FROM antenna_tbl a INNER JOIN rf_modes_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = 1 ORDER BY a.Antenna ASC;
SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = 1;
SELECT * FROM antenna_tbl a LEFT JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = 1;
