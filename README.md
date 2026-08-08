# GEPS - Gestión Eficiente Para Semilleros

Aplicativo web para la gestión integral de semilleros de investigación, proyectos, integrantes y eventos dentro de los programas de formación del SENA.

Proyecto académico desarrollado en equipo, propuesto y evaluado por la instructora del programa, orientado a resolver una necesidad real de organización y seguimiento dentro de los semilleros de investigación.

---

## 🎯 Problema que resuelve

Los semilleros de investigación necesitan un sistema centralizado para gestionar proyectos, integrantes, reuniones y eventos, con distintos niveles de acceso según el rol de cada usuario dentro del proceso investigativo. GEPS organiza todo este flujo en un solo aplicativo con roles diferenciados y reportes actualizados.

---

## 👥 Roles y funcionalidades

### Administrador
- Gestión completa (CRUD) de semilleros: creación, edición y desactivación
- Asignación de líderes de investigación a cada semillero
- Visualización de reportes actualizados de **todos** los semilleros

### Líder Investigador
- Gestión de proyectos, investigadores, reuniones y eventos de su semillero
- Creación de fases y actividades dentro de cada proyecto
- Asignación de investigadores a su semillero
- Visualización de reportes de su propio semillero

### Investigador
- Consulta de información de su semillero y lo relacionado con este

---

## 🤖 Integración con Inteligencia Artificial

El aplicativo integra la API de **Gemini** para asistir al administrador en la creación de semilleros: a partir del nombre y la línea investigativa, la IA genera automáticamente una propuesta de descripción, que el administrador puede editar libremente o dejar tal como fue generada.

---

## 🛠️ Tecnologías utilizadas

- **Backend:** ASP.NET (patrón MVC)
- **Vistas:** Razor (C# + HTML)
- **Base de datos:** MongoDB
- **Lenguaje principal:** C#
- **Frontend:** HTML, CSS, JavaScript
- **Reportes:** iTextSharp (generación de reportes en PDF)
- **IA:** API de Gemini
- **Notificaciones:** integración con Gmail para envío automático de credenciales

---

## 💡 Aporte de cada integrante

### Jean Julio

- Diseño y modelado de la **base de datos** en MongoDB
- Desarrollo completo del **panel de administrador** (gestión de semilleros y líderes)
- Diseño general del aplicativo
- Implementación de la **generación de reportes en PDF** con iTextSharp
- Implementación de la **integración con Gmail** para el envío automático de credenciales: cuando el administrador crea un líder, o el líder crea un investigador, el sistema envía las credenciales de acceso por correo electrónico automáticamente
- Ejecución de **pruebas** del sistema
- Elaboración del **plan de proyecto**

### Carlos Alfredo Velásquez

- Desarrollo completo del **panel del Líder Investigador** (gestión de proyectos, investigadores, reuniones, eventos, fases y actividades)
- Desarrollo completo del **panel del Investigador**

---

## 🧪 Pruebas

Las pruebas funcionales de todo el sistema se realizaron con Postman. Puedes ver la colección exportada aquí: [Colección Postman - GEPS](https://github.com/cvelasquezc-art/GEPS/blob/main/postman/GEPS.postman_collection.json)

---

## 📌 Contexto

Este proyecto fue desarrollado como trabajo grupal evaluativo dentro del programa de formación SENA, con el objetivo de demostrar la capacidad de diseñar y construir una solución funcional a un problema real. Actualmente no se encuentra en producción en el centro de formación.

---

## 👨‍💻 Autores

- Jean Julio ([@jeanjjuliio-netizen](https://github.com/jeanjjuliio-netizen))
- Carlos Alfredo Velásquez ([@cvelasquezc-art](https://github.com/cvelasquezc-art))
