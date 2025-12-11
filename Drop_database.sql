-- 1. Borrar la base de datos actual
DROP DATABASE IF EXISTS dm;

-- 2. (Opcional) Borrar el usuario si quieres realmente empezar desde cero
DROP USER IF EXISTS 'dm_user'@'localhost';
FLUSH PRIVILEGES;
