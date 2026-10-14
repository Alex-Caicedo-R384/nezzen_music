# Instrucciones para agentes — Nezzen Music

Responder en español. Inspeccionar estado Git, archivos e instrucciones antes de modificar. Trabajar en develop; no realizar commits, push, merges, despliegues ni cambios remotos sin autorización explícita.

## Arquitectura
Core no depende de otros proyectos. Application depende de Core. Infrastructure implementa contratos de Application. Audio depende de Core. Desktop compone Application, Infrastructure y Audio; no colocar lógica de dominio en ventanas.

## Convenciones
C# con nullable y usings implícitos, PascalCase en API pública, camelCase en variables; async con sufijo Async y CancellationToken. Identificadores Guid estables, fechas UTC, rutas absolutas normalizadas según SO. Dependencias mínimas; documentar decisiones en docs/ARCHITECTURE.md. No añadir dependencias circulares.

## Pruebas
Ejecutar dotnet build y dotnet test, o Docker Compose sin SDK local. Probar dominio e integración con SQLite temporal. Nunca usar bibliotecas musicales reales en tests; no afirmar verificaciones no ejecutadas. Revisar cambios antes de terminar.

## Seguridad y límites
Escaneos de solo lectura sobre archivos musicales. No eliminar registros al desconectarse una unidad. No escribir etiquetas, borrar música ni sobrescribir configuración existente sin inspección y respaldo. Nunca exponer credenciales. No instalar herramientas globales ni modificar sistema sin autorización. No copiar recursos de MusicBee, sortear DRM ni prometer migración sin verificar formatos.

La fase 2 incorpora el escáner incremental, vista previa y aplicación controlada. AnalyzeAsync no debe mutar biblioteca ni historial; ApplyAsync requiere revalidación y una transacción con revisión optimista. Preservar datos previos tras error de metadatos y reintentar. No interpretar enumeración parcial como Missing, seguir enlaces ni fusionar por etiquetas. No implementar motor completo, DSP, IA, editor Cinema, sincronización ni carga dinámica de plugins. No continuar fase 3 automáticamente. Las futuras tareas deben respetar REQUIREMENTS y ROADMAP. Usar Context7 para documentación actual necesaria, Playwright solo para UI web, Figma si hay referencias conectadas y Sentry si se investigan errores conectados.
