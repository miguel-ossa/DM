CREATE DATABASE dm CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'dm_user'@'localhost' IDENTIFIED BY 'dm_password';
GRANT ALL PRIVILEGES ON dm.* TO 'dm_user'@'localhost';
FLUSH PRIVILEGES;
