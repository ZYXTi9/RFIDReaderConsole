SELECT * FROM antenna_info_tbl;
SELECT * FROM antenna_tbl;
SELECT * FROM gpi_tbl;
SELECT * FROM gpo_tbl;
SELECT * FROM power_radio_tbl;
SELECT * FROM read_tbl;
SELECT * FROM reader_settings_tbl;
SELECT * FROM reader_tbl;
SELECT * FROM rf_modes_tbl;
SELECT * FROM singulation_tbl;
SELECT * FROM tag_storage_tbl;

call truncate_all_tables();

INSERT INTO read_tbl (AntennaID, EPC, TimeIn, Date, TimeOut, LogActive) VALUES (1, '103ewqweqw', TIME_FORMAT(NOW(), '%h:%i:%s %p'), DATE_FORMAT(NOW(), '%b %d, %Y'), TIME_FORMAT(NOW(), '%h:%i:%s %p'), 'Yes');

UPDATE read_tbl SET TimeOut = TIME_FORMAT(NOW(), '%h:%i:%s %p'), LogActive = 'No' WHERE LogActive = 'Yes';