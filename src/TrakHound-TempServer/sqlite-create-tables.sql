-- Create the Samples table
CREATE TABLE IF NOT EXISTS `samples` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,
`agent_instance_id` bigint NOT NULL,
`sequence` bigint NOT NULL,
`cdata` varchar(1000),
`condition` varchar(1000),

PRIMARY KEY (`device_id`, `id`, `timestamp`)
);

-- Create the Connections table
CREATE TABLE IF NOT EXISTS `connections` (

`device_id` varchar(90) NOT NULL,
`address` varchar(90) NOT NULL,
`port` int(10),
`physical_address` varchar(90),

PRIMARY KEY (`device_id`)
);

-- Create the Agents table
CREATE TABLE IF NOT EXISTS `agents` (

`device_id` varchar(90) NOT NULL,
`instance_id` bigint NOT NULL,
`sender` varchar(90),
`version` varchar(90),
`buffer_size` varchar(90),
`test_indicator` varchar(90),
`timestamp` bigint NOT NULL,

PRIMARY KEY (`device_id`)
);

-- Create the Devices table
CREATE TABLE IF NOT EXISTS `devices` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`id` varchar(90) NOT NULL,
`uuid` varchar(90),
`name` varchar(90),
`native_name` varchar(90),
`sample_interval` double,
`sample_rate` double,
`iso_841_class` varchar(90),
`manufacturer` varchar(180),
`model` varchar(180),
`serial_number` varchar(180),
`station` varchar(180),
`description` varchar(500),

PRIMARY KEY (`device_id`)
);

-- Create the Components table
CREATE TABLE IF NOT EXISTS `components` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`id` varchar(90) NOT NULL,
`uuid` varchar(90),
`name` varchar(90),
`native_name` varchar(90),
`sample_interval` varchar(90),
`sample_rate` varchar(90),
`type` varchar(90),
`parent_id` varchar(90) NOT NULL,

PRIMARY KEY (`device_id`, `id`)
);

-- Create the DataItems table
CREATE TABLE IF NOT EXISTS `data_items` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`id` varchar(90) NOT NULL,
`name` varchar(90),
`category` varchar(90),
`type` varchar(90),
`sub_type` varchar(90),
`statistic` varchar(90),
`units` varchar(90),
`native_units` varchar(90),
`native_scale` varchar(90),
`coordinate_system` varchar(90),
`sample_rate` varchar(90),
`representation` varchar(90),
`significant_digits` varchar(90),
`parent_id` varchar(90) NOT NULL,

PRIMARY KEY (`device_id`, `id`)
);
