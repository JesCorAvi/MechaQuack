# 🦆 MECHAQUACK - GUÍA DE ARQUITECTURA Y PROYECTO

> Archivo de contexto del proyecto para agentes de IA (**GEMINI.md** / **AGENTS.md**).
> Este documento resume la visión, arquitectura, jerarquías, sistemas mecánicos y convenciones técnicas para garantizar coherencia en futuras modificaciones.

---

## 1. 📖 Visión y Premisa del Juego

**MechaQuack** es un videojuego cómico y táctico en 3D desarrollado en **Unity 6 (6000.3.8f1)** con **Universal Render Pipeline (URP)**.

- **Premisa**: Un patito de goma pilota un Mecha bípede colosal gigante de **50 metros de altura** desde el interior de un torso blindado de tres plantas transitables conectado exclusivamente por escaleras físicas industriales.
- **Mecánica Central de Control**: El pato no dispone de mandos convencionales; para controlar la locomoción y el arsenal del mecha debe **correr, saltar y dar culazos/pisotones** directamente sobre botones físicos gigantes instalados en las plataformas de la cabina.
- **Proporciones Anatómicas de Mecha Japonés Esbelto (50m - Estilo Real Robot / Gundam / Armored Core)**:
  - **Piernas Largas (60% de la altura)**: Pelvis a $Y = 20.5\text{m}$, caderas separadas limpiamente a $X = \pm 6.0\text{m}$ con cazoletas protectoras (sockets) que envuelven las rótulas de cadera. Fémur estilizado de $10.5\text{m}$ ($3.8\text{m}$ ancho $\times 9.8\text{m}$ alto $\times 4.4\text{m}$ fondo) y tibia de $10.0\text{m}$ ($4.4\text{m}$ ancho $\times 9.5\text{m}$ alto $\times 5.2\text{m}$ fondo) con alcance total de $20.5\text{m}$ para una altura de reposo de $20.5\text{m}$, produciendo una flexión atlética natural de rodilla hacia adelante ($\sim 25^\circ-28^\circ$). Rodilleras afiladas de $4.6\text{m} \times 1.5\text{m} \times 3.0\text{m}$ en azul cobalto con cilindros mecánicos de articulación visibles en acero oscuro. Botas de combate macizas Hazard Stomper ($5.2\text{m}$ ancho $\times 2.4\text{m}$ alto $\times 7.5\text{m}$ largo) con empeine acorazado, puntera biselada, guardas de tobillo y franjas hazard, reposando exactamente a $Y = 0.0\text{m}$ sobre el suelo.
  - **Cintura Estrecha y Pelvis Limpia (8.5m)**: Cintura mecánica de 8.5m de ancho con pistones abdominales visibles en acero oscuro y cromo, y placa frontal compacta de $2.4\text{m} \times 2.2\text{m} \times 0.3\text{m}$ en azul cobalto liso. Cero faldones rojos gigantescos o desproporcionados.
  - **Torso en "V" Agresivo (8.5m a 16.5m)**: Nace estrecho en la cintura (8.5m) y se abre en diagonal ascendente hasta 16.5m en el pecho superior. Quilla pectoral biselada hacia adelante (como la proa de un caza espacial), tomas de aire frontales amarillas/naranjas (*chest vents*) y visores empotrados (tronera P1 y parabrisas panorámico P2 integrados en el bisel).
  - **Hombreras Tácticas Compactas (Cubic Shoulder Guards)**: Hombros limpios con hombreras cúbicas mecha en gris grafito ($3.4\text{m} \times 1.5\text{m} \times 3.8\text{m}$) adosadas a la articulación en $X = \pm 11.5\text{m}$, sin paneles superiores flotantes tipo tejado ni alas desproporcionadas.
  - **Arsenal Articulado Esbelto y Simétrico**: Brazo izquierdo con ametralladora rotatoria Gatling de 6 cañones cromados con tambor hazard; hombro derecho con pod lanzamisiles aerodinámico de 4 tubos con cabezas cian; y brazo derecho completo de combate con manipulador articulado (muñeca con pistones, dorso blindado azul/blanco, 4 dedos mecánicos y pulgar opuesto), escudo táctico lateral (*Tactical Strike Shield* con cresta dorada y franjas hazard) y cañón de pulso gemelo (*Forearm Pulse Cannon*) integrado bajo el antebrazo.
  - **Cabeza Icónica de Mecha Japonés con V-Fin Conectada al Torso**: Casco facetado en blanco y azul, visor óptico en V cian emisivo, barbilla carmesí y la antena dorada en V (*V-Fin Antenna*) coronando la silueta a más de 50m. Cuello mecánico sólidamente encastrado en el techo superior del torso ($Y = 12.0\text{m}$) mediante un collarín blindado trapezoidal en azul cobalto ($4.5\text{m} \times 1.8\text{m} \times 4.5\text{m}$), anillo de articulación de $4.2\text{m}$, cilindro de cuello de $2.8\text{m} \times 1.2\text{m} \times 2.8\text{m}$ y amortiguadores hidráulicos cromados, eliminando cualquier espacio vacío o cabeza flotante.
- **Técnica "TARDIS" de Desacoplamiento Interior vs Exterior (0.48x Cockpit / 0.38x Duck)**:
  - El complejo interior completo de 3 plantas, escaleras, botones, consolas decorativas y el propio pato se agrupan en el contenedor `Cockpit` escalado a **0.48x (`Vector3.one * 0.48f`)**, con el Pato (`DuckController`) a escala local **0.38x**.
  - **Huella Física Compacta en el Mundo Real**: En el espacio del mundo, las 3 plantas solo ocupan $11.52\text{m} \times 8.64\text{m}$, encajando con holgura dentro del torso en V de 16.5m sin que ningún forjado ni borde de suelo sobresalga.
  - **Amplitud Interior Gigantesca Intacta**: Para el pato en el interior, el espacio sigue midiendo $24\text{m} \times 18\text{m}$ por planta con techos de 3.8m, 8.2m y 14.5m, ofreciendo un recorrido x2.5 más grande y navegación vertical continua por escaleras de 3.0m.
- **Complejo Interior Vertical de 3 Plantas ($24\text{m} \times 18\text{m}$)**:
  - **Planta 1 (Nivel 1 - Y = 0.0m)**: *"Búnker Dieselpunk y Sala de Calderas" (Verde Militar, Óxido, Ámbar y Acero Remachado)*. Estación de Navegación del Conductor (marcha adelante, atrás y giros sobre su eje). Muros inclinados remachados, 4 pistones hidráulicos en las esquinas ($X = \pm 10.5\text{m}, Z = \pm 7.5\text{m}$), **Núcleo de Reactor de Fusión Industrial** en popa ($Z < -4.5\text{m}$) con anillos emisivos y tuberías hacia los botones, **Fachada frontal con estructura de búnker/tanque de 4 piezas** alrededor de una abertura central despejada ($X \in [-8.0\text{m}, +8.0\text{m}]$, $Y \in [0.9\text{m}, 2.2\text{m}]$): faldón glacis inferior inclinado a $22^\circ$ ($Y \le 0.9\text{m}$), glacis superior inclinado a $25^\circ$ ($Y \ge 2.2\text{m}$), pilares blindados laterales en $X = \pm 10.0\text{m}$ (ancho 4.0m), y cristal central `Slit_Visor_Glass` ($16.0\text{m} \times 1.3\text{m} \times 0.1\text{m}$) transparente al 92% a $Y = 1.55\text{m}$, complementado con consola de navegación de perfil bajo ($Y \le 0.60\text{m}$) que garantiza una línea de visión 100% despejada hacia el terreno. Techo físico en $Y = 3.8\text{m}$.
  - **Planta 2 (Nivel 2 - Y = 4.5m)**: *"Hangar de Artillería y Cubierta Panorámica" (Gris Naval, Amarillo Hazard y Cian Criogénico)*. Estación de Artillería y Arsenal. Gran ventanal frontal panorámico de 20m al 96% de transmisión óptica y ventanales laterales hacia las armas, suelo con textura de cubierta naval antideslizante, rieles de grúa aérea y conductos de munición hacia los hombros, tanques criogénicos con manómetros, plataforma giroscópica de puntería analógica 2D (`AIM_PAD`, escala $\times 1.2$ en $(0, 4.54, +5.2)$), HUD Holográfico (`WindshieldHUDCrosshair` a $Y = 6.8\text{m}$), disparador Gatling, pod lanzamisiles guiados y consola frontal de perfil bajo ($Y < 0.6\text{m}$). Techo físico en $Y = 8.2\text{m}$.
  - **Planta 3 (Nivel 3 - Y = 9.0m)**: *"Santuario Neuronal del Capitán" (Negro Fibra de Carbono, Blanco, Oro y Hologramas Cian)*. Cabeza del Mecha / Cápsula de Mando del Capitán. Estructura blindada sci-fi sin ventanas con cúpula abovedada en $Y = 14.5\text{m}$, muros con fibra de carbono hexagonal, 7 costillas arqueadas luminosas cian, racks de supercomputadoras neurales, y botón de invasión (`SPAWN`, $Z = -3.2\text{m}$). Pedestal Central de Mando del Capitán (`Captain_Command_Pedestal`, $X = 0, Y = 9.2, Z = +1.5, R = 2.2\text{m}$) con círculo holo-disc y anillos holográficos flotantes.
- **Exclusividad y Automatismo de Cámara en 3ª Persona**: El Conductor (P1) y el Artillero (P2), así como el Capitán fuera de su pedestal, operan con la cámara **estrictamente bloqueada en el interior de la cabina** (`maxInteriorDistance = 5.2f`, zoom interior por defecto `4.2f`). Al entrar físicamente al círculo marcado de mando en Planta 3 ($R = 2.2\text{m}$), la cámara **pasa AUTOMÁTICAMENTE a Tercera Persona Exterior del Titán de 50m** con zoom exterior de hasta 160m y encuadre cinemático superior. Al salir del círculo marcado o descender hacia Planta 2 o 1, la cámara **se repliega de forma automática e inmediata al interior** (`TargetDistance <= 5.2f`).
- **Entorno**: Megaterreno post-apocalíptico de 1200m x 1200m con pórtico gantry de 68m de altura libre, rascacielos derruidos de hasta 165m, desniveles rocosos, un cañón de ácido tóxico corrosivo de 440m y oleadas de robots enemigos hostiles (`EnemyBot`).

---

## 2. 🛠️ Stack Tecnológico y Reglas del Proyecto

- **Motor**: Unity 6 LTS (`6000.3.8f1`) con Universal Render Pipeline (URP).
- **Estructura Estándar de Prefabs (`Assets/Prefabs/`)**: Todos los elementos del juego (Mecha, Cúpula, Pato, Botones, Consolas, Enemigos, Armas) están estructurados como prefabs reutilizables en carpetas estándar de Unity (`Prefabs/Mecha`, `Prefabs/Cockpit`, `Prefabs/Characters`, `Prefabs/Props/Buttons`, `Prefabs/Props/Consoles`, `Prefabs/Enemies`, `Prefabs/Weapons`).
- **Filosofía "Zero External Assets"**: Todo el arte, texturas PBR, geometría, efectos visuales y audio se generan **proceduralmente** mediante primitivas 3D optimizadas, shaders de URP y síntesis matemática de ondas en runtime. **No añadir dependencias binarias externas**.
- **Persistencia Real en Disco de Texturas (.png) y Materiales (.mat)**:
  - Todas las texturas procedurales se guardan como archivos `.png` reales en `Assets/Textures/Generated/` mediante `ImageConversion.EncodeToPNG()` y se configuran vía `TextureImporter`.
  - Todos los materiales se serializan como `.mat` reales en `Assets/Materials/Generated/` (y espejo en `Assets/Materials/`) usando `Shader.Find("Universal Render Pipeline/Lit")`.
  - Se vincula el asset de disco recargado a `_BaseMap` y `mainTexture` para preservar referencias tras serialización de prefabs y cambios de escena, eliminando totalmente los materiales rosas/magenta o texturas blancas perdidas.
  - Tiling ajustado (`mat.mainTextureScale` / `_BaseMap_ST`) para máxima definición en chapas estriadas, franjas hazard, remaches y fibra de carbono.
- **Interfaz (UI)**: Desarrollada con **UI Toolkit** (`UIDocument`, `MechaHUD.uxml`, `MechaHUD.uss`, y `MechaFontAsset.asset` con fuentes SDF), incluyendo badge de telemetría de planta dinámica.
- **Sistema de Entrada**: Unity New Input System (`com.unity.inputsystem`) soportando Teclado, Ratón y Gamepad.
- **Rendimiento y Memoria**: Cero allocations por frame en bucles críticos (`Update`, `FixedUpdate`). Uso estricto de buffers prealocados para físicas (`Physics.RaycastNonAlloc`, `Physics.SphereCastNonAlloc`).

---

## 3. 🕹️ Esquema de Controles y Distribución Vertical en 3 Plantas

### Cabina del Mecha (Pato a Escala 0.4x)
- **`W`, `A`, `S`, `D` / Flechas**: Mover al pato por el suelo y escaleras de la cabina (`moveSpeed = 14.0m/s`, aceleración 70.0 m/s²).
- **`Espacio`**: Saltar entre plataformas, peldaños y botones físicos (`jumpForce = 8.2m/s`).
- **`E` / `Shift` / Clic Izquierdo (en el aire)**: **¡CULAZO! (Stomp)**. Pisotón en picado (`stompSpeed = 24.0m/s`) que activa botones especiales de impacto y armas.
- **`Q` / Clic Derecho**: **¡CUAC!** Graznido cómico con efecto de sonido, aleteo y texto flotante.

- **Conexión Exclusiva por Escaleras Físicas**:
  - **Prohibido el uso de teletransportadores o atajos de teclado para cambiar de planta**.
  - Los patos transitan subiendo y bajando por **escaleras físicas de 3.0m de paso libre** con peldaños hazard, barandillas dobles y un **colisionador de rampa invisible suave** que asegura una carrera continua sin atascos.
  - Tramo 1 (Planta 1 -> Planta 2): en estribor ($X = +7.5\text{m}$), de $Y = 0.0\text{m}$ a $Y = 4.5\text{m}$ ($Z \in [-4.5\text{m}, +3.5\text{m}]$, ancho $2.8\text{m}$).
  - Tramo 2 (Planta 2 -> Planta 3): en babor ($X = -7.5\text{m}$), de $Y = 4.5\text{m}$ a $Y = 9.0\text{m}$ ($Z \in [+3.5\text{m}, -4.5\text{m}]$, ancho $2.8\text{m}$).

### Distribución de Botones Físicos en las 3 Plantas (`CockpitFloorButton`)

```
==================================================================================
PLANTA 3 (Y = 9.0m) - SANTUARIO NEURONAL DEL CAPITÁN [3ª PERSONA AUTOMÁTICA]
   - Cabeza del Mecha / Cúpula ciega (Y = 14.5m, sin ventanas, fibra hexagonal sci-fi)
   - Pedestal Central de Mando del Capitán (X: 0.0, Y: 9.2, Z: +1.5) con holo-disc (R = 2.2m)
   - Consola Táctica de Mando (X: 0.0, Y: 9.25, Z: +5.2, Y < 0.6m)
   - [🚨 SPAWN] (X: 0.0, Y: 9.0, Z: -3.2) -> Conmutador Invasión Robots (Stomp / Toggle)
   ▲ Escalera 2 que desciende hacia Planta 2 en babor (X: -8.5m, ancho 3.0m)
==================================================================================
PLANTA 2 (Y = 4.5m) - HANGAR DE ARTILLERÍA Y CUBIERTA PANORÁMICA [MÁXIMA VISIBILIDAD]
   - Gran ventanal frontal de 20.0m al 96% transparente y ventanas hacia hombros
   - Baterías criogénicas de refrigerante, tuberías y conductos a los hombros
   - HUD Holográfico de Puntería en parabrisas (Y = 6.8m)
   - [🎯 AIM PAD] (X: 0.0, Y: 4.54, Z: +5.2) -> Giróscopo 2D centralizado (Escala x1.2)
   - [🔫 GATLING] (X: -4.2, Y: 4.50, Z: +1.6) -> Ráfaga Gatling (Hold / Stomp) en ala babor
   - [🚀 MISSILES] (X: +4.2, Y: 4.50, Z: +1.6) -> Salva Micromisiles Guiados (Stomp) en ala estribor
   ▲ Escalera 1 que desciende hacia Planta 1 en estribor (X: +8.5m, ancho 3.0m)
==================================================================================
PLANTA 1 (Y = 0.0m) - BÚNKER DIESELPUNK Y SALA DE CALDERAS [BÚNKER BLINDADO MILITAR]
   - Tronera militar estrecha (14.0m x 1.0m) a nivel de ojos
   - Muros remachados, 4 pistones esquineros (X: +-10.5m, Z: +-7.5m)
   - Reactor de Fusión Industrial en popa (Z < -4.5m) con tuberías hacia los mandos
   - Consola de Navegación central con pantalla radar CRT militar verde (X: 0.0, Z: +7.2)
   - [▲ FWD] (X: 0.0, Y: 0.0, Z: +4.2) -> Marcha hacia adelante
   - [▼ BWD] (X: 0.0, Y: 0.0, Z: -2.2) -> Marcha hacia atrás
   - [◄ LEFT] (X: -4.2, Y: 0.0, Z: +1.0) -> Giro sobre eje a la izquierda
   - [RIGHT ►] (X: +4.2, Y: 0.0, Z: +1.0) -> Giro sobre eje a la derecha
==================================================================================
```

| Nivel | Botón ID | Coordenadas Locales | Tipo | Función |
| :--- | :--- | :--- | :--- | :--- |
| **Planta 1** | **`FWD`** | `(0.0, 0.0, +4.2)` | `Hold` | Marcha hacia adelante del mecha |
| **Planta 1** | **`BWD`** | `(0.0, 0.0, -2.2)` | `Hold` | Marcha hacia atrás del mecha |
| **Planta 1** | **`LEFT`** | `(-4.2, 0.0, +1.0)`| `Hold` | Rotación sobre su eje a la izquierda |
| **Planta 1** | **`RIGHT`**| `(+4.2, 0.0, +1.0)`| `Hold` | Rotación sobre su eje a la derecha |
| **Planta 2** | **`AIM_PAD`**| `(0.0, 4.54, +5.2)`| `Hold` / `Stomp` | **Giroscopio de Puntería 2D (Escala x1.2)**: Centralizado frente al HUD crosshair y ventanal. Correr sobre la plataforma inclina analógicamente la mira y gimbals. **Culazo (Stomp)** ejecuta **Smart Lock-On** al enemigo más cercano. |
| **Planta 2** | **`GATLING`**| `(-4.2, 4.50, +1.6)`| `Hold` / `Stomp` | **Fuego continuo orientado** al pisar (12 bps) o **ráfaga masiva de 10 tiros** al dar culazo, en ala babor |
| **Planta 2** | **`MISSILES`**| `(+4.2, 4.50, +1.6)`| `Stomp` | **Salva de 4 micromisiles guiados** lanzados desde el pod orientable, en ala estribor (Cooldown 3.5s) |
| **Planta 3** | **`SPAWN`** | `(0.0, 9.00, -3.2)` | `Toggle` | Activa/desactiva la invasión de robots enemigos (requiere culazo en el Santuario del Capitán) |

---

## 4. 🧩 Arquitectura de Sistemas y Scripts

### A. Locomoción Física y Estabilidad del Mecha Colosal (50m Esbelto)
- [`MechaController.cs`](file:///Assets/Scripts/MechaController.cs):
  - Rigidbody dinámico (masa 48,000 kg, altura 50.0m, pelvis a $Y = 22.0\text{m}$, collider $17\text{m} \times 26\text{m} \times 13\text{m}$).
  - Suspensión muelle-amortiguador con `RaycastNonAlloc` (`targetRideHeight = 20.5m`, `legRayLength = 25.0m`, `suspensionStrength = 1,900,000 N/m`, `suspensionDamping = 230,000 N·s/m`).
  - Fuerza de empuje: `movePower = 1,250,000 N`, `turnTorque = 950,000 N·m`, velocidad máxima calibrada a 22 m/s.
  - Centro de masas: `(0, 6.2m, 0.05m)` con tensor de inercia reforzado `(880000, 500000, 950000)`.
  - **Físicas de Caída y Robot Torpe (`IsAirborne`)**: Gravedad extra de caída (`extraFallGravity = 17.5 m/s²`), amortiguación aérea mínima (`linearDamping = 0.05f`) y torsión oscilante cómica.
  - **Impacto de Aterrizaje Brutal (`HandleLandingImpact`)**: Landing squash elástico hasta 2.8m, congelación temporal de 65ms para impactos >14 m/s, trauma cuadrático de sacudida y sonido sub-grave en [`ProceduralAudio.PlayCrashLand()`](file:///Assets/Scripts/ProceduralAudio.cs).
  - Control de vuelco: si la inclinación supera 60°, entra en estado `isOverturned`.
  - Maniobra **¡PATALETA HIDRÁULICA! (`RecoverFromTumble`)**: Salto propulsor vertical de 28 m/s que endereza el mecha al hacer culazo o saltar cuando está volcado.
  - **Resistencia a Hot-Reload**: Método `EnsureBalanceModel()` invocado en `Awake()`, `OnEnable()` y `FixedUpdate()` para evitar pérdida de estado tras recompilaciones.
- [`ProceduralLeg.cs`](file:///Assets/Scripts/ProceduralLeg.cs) & [`MechBodyStabilizer.cs`](file:///Assets/Scripts/MechBodyStabilizer.cs):
  - Two-Bone IK analítico con proporciones japonesas: caderas en $X = \pm 6.0\text{m}$ (postura atlética separada), fémur de $10.5\text{m}$ con armadura blanca, rodillera afilada sobresaliente hacia arriba de $4.6\text{m} \times 1.5\text{m} \times 3.0\text{m}$ en azul cobalto con articulación cilíndrica visible, tibia de $10.0\text{m}$ en azul cobalto con guardas de tobillo anguladas, `stepDistance = 6.0m`, `stepHeight = 2.2m`, `maxClimbableStepHeight = 5.0m`, botas de combate estilizadas con volumen vertical de $5.2\text{m} \times 2.4\text{m} \times 7.5\text{m}$. Pole target desacoplado a nivel del mecha para máxima estabilidad rotacional.

### B. Control del Pato y Físicas Multinivel (Desacoplamiento TARDIS 0.48x Cockpit / 0.38x Duck)
- [`DuckController.cs`](file:///Assets/Scripts/DuckController.cs):
  - Escala local de cabina: `cockpitHalfX = 11.8f`, `cockpitHalfZ = 8.8f`, `cockpitMaxY = 16.5f`, `duckRadius = 0.22f`.
  - Pato configurado a escala local **0.38x (`Vector3.one * 0.38f`)**.
  - Velocidad calibrada a `moveSpeed = 14.0m/s` con aceleración de 70 m/s².
  - Detección continua de suelo y escalones mediante buffer prealocado `Physics.RaycastNonAlloc` con `stepSnapOffset = 0.45f`.
  - Escaleras industriales de 2.8m de paso (Tramo 1 en estribor $X = +7.5\text{m}$, Tramo 2 en babor $X = -7.5\text{m}$).
  - **Físicas Estrictas Anti-Salto entre Plantas**:
    - Fuerza de salto calibrada a `jumpForce = 8.2m/s`.
    - Techo físico dinámico (`GetCeilingCenterY`): Clamping de techo para Planta 1 (3.8m), Planta 2 (8.2m) y Planta 3 (14.5m).
    - Detección de suelo por raycast con filtrado de planta: si el pato no se encuentra dentro de las cajas de escalera (`IsInStairwell1`, `IsInStairwell2`), se descarta cualquier superficie de plantas superiores.
    - Fallback de planta conservador: conserva la planta actual del pato y previene cualquier teletransporte hacia arriba.
  - Detección de planta en tiempo real (`CurrentFloorLevel = 1, 2 ó 3` y `CurrentFloorName`).
  - **Aislamiento Vertical de Botones**: Validación tridimensional estricta en `CockpitFloorButton` (`|Y_pato - Y_boton| < 1.6f`) para evitar activaciones accidentales entre plantas superpuestas.

### C. Sistema de Cámaras: Exclusividad del Capitán
- [`CockpitCameraController.cs`](file:///Assets/Scripts/CockpitCameraController.cs):
  - **Plantas 1 y 2 (Conductor y Artillero)**:
    - Cámara **ESTRICTAMENTE BLOQUEADA EN EL INTERIOR**.
    - Distancia máxima de zoom restringida a `maxInteriorDistance = 5.2f` (distancia por defecto 4.2m).
    - Modo espectador / dron (`C`) desactivado.
    - Sistema Push-In anti-recorte adaptado al volumen interior ($24\text{m} \times 18\text{m}$ con $X \in [\pm 11.4\text{m}]$, $Z \in [\pm 8.4\text{m}]$).
  - **Planta 3 (Puente del Capitán)**:
    - En el suelo, escaleras y consola: cámara bloqueada en **vista interior**.
    - Al entrar en el **Círculo Marcado del Pedestal** (`Captain_Command_Pedestal`, $X = 0, Y = 9.2, Z = +1.5, R = 2.2\text{m}$): se desbloquea y activa la visión en **Tercera Persona Exterior** del Mecha colosal de 50 metros.
    - Zoom exterior en metros del mundo real con rueda de ratón hasta 160.0m y modo dron (`C`) habilitados exclusivamente dentro del círculo.
    - **Replegado Automático**: Al salir del círculo marcado o descender hacia Planta 2 o Planta 1, la cámara se repliega de forma inmediata y automática al interior de la cabina (`targetDistance = maxInteriorDistance = 5.2f`).

### D. Arsenal Orientable y Chasis Real Robot
- [`GunnerAimPad.cs`](file:///Assets/Scripts/GunnerAimPad.cs) & [`SetupGunnerAimPad.cs`](file:///Assets/Scripts/Editor/SetupGunnerAimPad.cs):
  - Plataforma giroscópica analógica de puntería instalada en Planta 2 (`(0.0m, 4.54m, +5.2m)` con escala visual $\times 1.2$).
  - Modulación suave de azimut ($\pm 48^\circ$) y elevación ($-20^\circ$ a $+36^\circ$) con retención de ángulo (*Aim Hold*).
  - Culazo ejecuta Smart Lock-On inmediato.
- **Brazo Izquierdo — Ametralladora Rotatoria Gatling (`Arm_Left_Gatling`)**:
  - Hombro en $X = -11.5\text{m}$, brazo esbelto articulado en $X = -11.5\text{m}, Y = 11.5\text{m}$. Gimbal articulado con 6 cañones cromados rotatorios, bicep con pistón chrome, antebrazo en blanco/azul y tambor inferior hazard.
  - [`GatlingTracer.cs`](file:///Assets/Scripts/GatlingTracer.cs): Proyectiles trazadores a 160 m/s sin GC allocations.
- **Hombro Derecho — Pod Lanzamisiles Orientable y Aerodinámico (`Missile_Pod`)**:
  - Montado en el soporte dorsal/hombro en $X = +7.8\text{m}, Y = 15.5\text{m}, Z = -0.8\text{m}$. Gimbal tridimensional orientable sincronizado con la mira del artillero, con 4 tubos y ojivas ópticas cian.
  - [`MissileProjectile.cs`](file:///Assets/Scripts/MissileProjectile.cs): Micromisiles guiados con motor cohete (82 m/s), autoguiado dual y daño en área (AOE 9.5m).
- **Brazo Derecho — Manipulador de Combate Real Robot, Escudo y Cañón de Pulso (`Arm_Right_MissilePod`)**:
  - Hombro en $X = +10.5\text{m}$, bíceps con pistón chrome y blindaje cobalto, codo de $2.8\text{m}$, antebrazo blanco/azul con muñeca articulada con pistones, mano robótica de Mecha japonés (dorso blindado azul/blanco, placa de nudillos dorada, 4 dedos mecánicos articulados en puño de combate y pulgar hidráulico opuesto), escudo táctico lateral (*Tactical Strike Shield*, $8.2\text{m} \times 3.4\text{m} \times 0.45\text{m}$) con franja hazard y cresta dorada, y cañón de pulso gemelo (*Forearm Pulse Cannon*) integrado bajo el antebrazo con ópticas cian emisivas. Equivale en longitud, silueta y presencia al brazo Gatling izquierdo.
- **Hombreras Tácticas Compactas (Cubic Shoulder Guards)**:
  - En $X = \pm 11.5\text{m}, Y = 13.5\text{m}$, hombreras cúbicas mecha en gris grafito de $3.4\text{m} \times 1.5\text{m} \times 3.8\text{m}$ adosadas directamente al hombro y brazo. Total eliminación de alerones flotantes superiores o placas de tejado.
- **Pelvis Frontal Compacta**:
  - Placa frontal estilizada de $2.4\text{m} \times 2.2\text{m} \times 0.3\text{m}$ en azul cobalto liso, sin faldones sobredimensionados ni texturas de cuadrícula/teja.

### E. HUD Holográfico y UI Toolkit
- [`WindshieldHUDCrosshair.cs`](file:///Assets/Scripts/WindshieldHUDCrosshair.cs):
  - Proyectado sobre el parabrisas de Planta 2 a `baseHeightY = 6.8f` con telémetro, horizonte artificial y retícula de puntería dinámica.
- [`MechaHUD.cs`](file:///Assets/Scripts/MechaHUD.cs) & UI Toolkit (`MechaHUD.uxml`, `MechaHUD.uss`):
  - Badge de telemetría de planta en la barra superior con transiciones de color:
    - *Planta 1*: Verde esmeralda (`PLANTA 1: CONDUCTOR [Búnker Dieselpunk - Guiado por Capitán]`).
    - *Planta 2*: Ámbar dorado (`PLANTA 2: ARTILLERO [Hangar de Artillería y Cubierta Panorámica]`).
    - *Planta 3*: Cian emisivo (`PLANTA 3: CABEZA DEL MECHA [Santuario Neuronal - 3ª Persona Activa]`).

### F. Herramientas de Editor y Generadores
- [`Assets/Scripts/ProceduralTextureGenerator.cs`](file:///Assets/Scripts/ProceduralTextureGenerator.cs):
  - Motor central de generación y síntesis de mapas de textura PBR guardados como `.png` reales en `Assets/Textures/Generated/` y materiales `.mat` persistentes en `Assets/Materials/Generated/` y `Assets/Materials/`.
  - Paleta Real Robot PBR: `M_MechaArmorWhite`, `M_MechaCobaltBlue`, `M_MechaInnerFrame`, `M_MechaCrimsonRed`, `M_MechaChestVent`, `M_MechaHazardYellow`, `M_MechaGoldVFin`.
- [`Assets/Scripts/Editor/MechaPrefabBuilder.cs`](file:///Assets/Scripts/Editor/MechaPrefabBuilder.cs):
  - Menú `Mecha > Build All Standard Prefabs`: Construye y serializa automáticamente todos los prefabs en `Assets/Prefabs/` (Mecha, Cockpit multinivel con escaleras, personajes con pato a 0.38x, botones, consolas, enemigos, armas).
- [`Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs`](file:///Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs):
  - Menú `Mecha > Generate Mega Proving Grounds`: Megaterreno procedural de 1200m x 1200m con pórtico gantry de 68m de altura libre.
- [`Assets/Scripts/Editor/AssembleMechaPrototype.cs`](file:///Assets/Scripts/Editor/AssembleMechaPrototype.cs):
  - Menú `Mecha > Assemble Complete Prototype`: Reensambla desde cero toda la escena con el Mecha Japonés de 50m, cintura de 8.5m con pistones, faldones a $35^\circ$, torso en V de 16.5m con quilla y tomas de aire amarillas, hombreras aladas de $25^\circ$, casco V-Fin, cabina interior desacoplada a 0.48x, reactor de fusión, ventanales panorámicos, santuario sci-fi, escaleras físicas, HUD y terreno.
- [`Assets/Scripts/AutoPlayTester.cs`](file:///Assets/Scripts/AutoPlayTester.cs):
  - Batería automatizada de **11 pruebas exhaustivas en modo Play** (Avance, Retroceso, Giros L/R, Gatling, Misiles, Aim Pad, Invasión Robots, Vuelco/Pataleta, UI Toolkit y Navegación Multinivel 3 Plantas con Exclusividad de Cámara). **11/11 Pruebas Verificadas con Éxito**.

---

## 5. ⚠️ Directrices Cruciales para Futuras Modificaciones

1. **Estructura Estándar de Prefabs**: Mantener y actualizar siempre los prefabs en `Assets/Prefabs/` (`Mecha`, `Cockpit`, `Characters`, `Props/Buttons`, `Props/Consoles`, `Enemies`, `Weapons`).
2. **Proporciones Anatómicas de Mecha Japonés (50m)**: NUNCA ensanchar el chasis exterior del Mecha para alojar la cabina directamente. Mantener cintura en 8.5m, pecho en V de 16.5m, caderas en $X = \pm 7.2\text{m}$, hombros en $X = \pm 11.5\text{m}$ y pelvis en $Y = 22.0\text{m}$ con fémur y tibia de $12.5\text{m}$. Mantener hombreras cúbicas $\le 3.5\text{m} \times 1.5\text{m} \times 4.0\text{m}$ y botas mecha estilizadas de $3.4\text{m} \times 1.8\text{m} \times 5.2\text{m}$. Prohibido añadir paneles flotantes superiores de tejado o faldones gigantes tipo ladrillo/teja.
3. **Técnica TARDIS de Desacoplamiento Interior (0.48x)**: El complejo interior de 3 plantas debe mantenerse en el contenedor `Cockpit` escalado a 0.48x con el pato a 0.38x. Los forjados y bordes de suelo interiores NUNCA deben sobresalir fuera de las placas de blindaje del pecho.
4. **Prohibido Teletransporte entre Plantas**: La navegación vertical de los patos DEBE realizarse exclusivamente a pie o mediante saltos sobre las escaleras físicas.
5. **Exclusividad de Cámara del Capitán**: NUNCA permitir cámara en tercera persona exterior ni modo dron cuando el pato se encuentre en Planta 1 o Planta 2, o fuera del círculo de mando en Planta 3 ($R = 2.2\text{m}$).
6. **Persistencia Real de Texturas y Materiales**: Guardar siempre texturas como `.png` reales en `Assets/Textures/Generated/` y materiales como `.mat` en `Assets/Materials/Generated/` usando shaders de Universal Render Pipeline (URP Lit). Prohibido crear instancias volátiles en memoria sin serializar.
7. **Filosofía Zero External Assets y Cero GC Allocations**: Utilizar únicamente primitivas 3D procedurales optimizadas, shaders nativos de URP y síntesis acústica y textural en runtime/editor. No alojar memoria en bucles `Update` ni `FixedUpdate`.
