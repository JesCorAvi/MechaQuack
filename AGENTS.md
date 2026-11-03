# 🦆 MECHAQUACK - GUÍA DE ARQUITECTURA Y PROYECTO

> Archivo de contexto del proyecto para agentes de IA (**GEMINI.md** / **AGENTS.md**).
> Este documento resume la visión, arquitectura, jerarquías, sistemas mecánicos y convenciones técnicas para garantizar coherencia en futuras modificaciones.

---

## 1. 📖 Visión y Premisa del Juego

**MechaQuack** es un videojuego cómico y táctico en 3D desarrollado en **Unity 6 (6000.3.8f1)** con **Universal Render Pipeline (URP)**.

- **Premisa**: Un patito de goma pilota un Mecha bípede colosal gigante de **50 metros de altura** desde el interior de un torso blindado de tres plantas transitables conectado exclusivamente por escaleras físicas industriales.
- **Mecánica Central de Control**: El pato no dispone de mandos convencionales; para controlar la locomoción y el arsenal del mecha debe **correr, saltar y dar culazos/pisotones** directamente sobre botones físicos gigantes instalados en las plataformas de la cabina.
- **Complejo Interior Vertical de 3 Plantas**:
  - **Planta 1 (Nivel 1 - Y = 0.0m)**: *Estación de Navegación del Conductor*. Marcha hacia adelante, atrás y giros sobre su eje. **Visibilidad deliberadamente reducida tipo Búnker Blindado** con tronera militar estrecha (`Slit_Visor_Glass`), muros gruesos y pantallas de navegación; el Conductor es guiado exclusivamente por la telemetría y órdenes del Capitán.
  - **Planta 2 (Nivel 2 - Y = 4.5m)**: *Estación de Artillería y Arsenal*. **La mejor visibilidad de la cabina**: gran ventanal frontal panorámico de 20m al 96% de transmisión óptica y ventanales laterales despejados hacia los brazos/hombros, plataforma giroscópica de puntería analógica 2D (`AIM_PAD`), HUD Holográfico (`WindshieldHUDCrosshair`), disparador Gatling, pod lanzamisiles guiados y cubierta de tiro totalmente diáfana y despejada.
  - **Planta 3 (Nivel 3 - Y = 9.0m)**: *Cabeza del Mecha / Cápsula de Mando del Capitán*. Estructura blindada de la cabeza del Mecha con pedestal central, interfaz neural holográfica cian y botón de invasión (`SPAWN`). **Visión interior dentro del puente**: mientras el pato camine por el suelo, escaleras o pulse el botón SPAWN, la cámara permanece en interior. Al posicionarse físicamente dentro del **CÍRCULO MARCADO** del pedestal (`Captain_Command_Pedestal`, $R = 2.2\text{m}$), la cámara **pasa AUTOMÁTICAMENTE a Tercera Persona Exterior** del Mecha Titán de 50m.
- **Exclusividad y Automatismo de Cámara en 3ª Persona**: El Conductor (P1) y el Artillero (P2), así como el Capitán fuera de su pedestal, operan con la cámara **estrictamente bloqueada en el interior de la cabina**. Al entrar físicamente al círculo marcado de mando en Planta 3, la cámara **pasa AUTOMÁTICAMENTE a Tercera Persona Exterior del Titán de 50m** con zoom de hasta 160m y encuadre cinemático superior. Al salir del círculo marcado o descender hacia Planta 2 o 1, la cámara **se repliega de forma automática e inmediata al interior**.
- **Entorno**: Megaterreno post-apocalíptico de 1200m x 1200m con pórtico gantry de 68m de altura libre, rascacielos derruidos de hasta 165m, desniveles rocosos, un cañón de ácido tóxico corrosivo de 440m y oleadas de robots enemigos hostiles (`EnemyBot`).

---

## 2. 🛠️ Stack Tecnológico y Reglas del Proyecto

- **Motor**: Unity 6 LTS (`6000.3.8f1`) con Universal Render Pipeline (URP).
- **Estructura Estándar de Prefabs (`Assets/Prefabs/`)**: Todos los elementos del juego (Mecha, Cúpula, Pato, Botones, Consolas, Enemigos) están estructurados como prefabs reutilizables en carpetas estándar de Unity (`Prefabs/Mecha`, `Prefabs/Cockpit`, `Prefabs/Characters`, `Prefabs/Props/Buttons`, `Prefabs/Props/Consoles`, `Prefabs/Enemies`).
- **Filosofía "Zero External Assets"**: Todo el arte, geometría, efectos visuales y audio se generan **proceduralmente** mediante primitivas 3D optimizadas, shaders de URP y síntesis matemática de ondas en runtime. **No añadir dependencias binarias externas** a menos que el usuario lo solicite expresamente.
- **Interfaz (UI)**: Desarrollada con **UI Toolkit** (`UIDocument`, `MechaHUD.uxml`, `MechaHUD.uss`, y `MechaFontAsset.asset` con fuentes SDF), incluyendo badge de planta actual con código de color dinámico.
- **Sistema de Entrada**: Unity New Input System (`com.unity.inputsystem`) soportando Teclado, Ratón y Gamepad.
- **Rendimiento y Memoria**: Cero allocations por frame en bucles críticos (`Update`, `FixedUpdate`). Uso estricto de buffers prealocados para físicas (`Physics.RaycastNonAlloc`, `Physics.SphereCastNonAlloc`).

---

## 3. 🕹️ Esquema de Controles y Distribución Vertical en 3 Plantas

### Cabina del Mecha (Pato)
- **`W`, `A`, `S`, `D` / Flechas**: Mover al pato por el suelo y escaleras de la cabina (movimiento relativo a la cámara con fricción y resistencia a la inclinación, velocidad calibrada a 14 m/s).
- **`Espacio`**: Saltar entre plataformas, peldaños y botones físicos (`jumpForce = 10.5m/s` para un salto ágil y preciso que nunca traspasa techos).
- **`E` / `Shift` / Clic Izquierdo (en el aire)**: **¡CULAZO! (Stomp)**. Pisotón en picado (`stompSpeed = 32m/s`) que activa botones especiales de impacto y armas.
- **`Q` / Clic Derecho**: **¡CUAC!** Graznido cómico con efecto de sonido, aleteo y texto flotante.

- **Conexión Exclusiva por Escaleras Físicas**:
  - **Prohibido el uso de teletransportadores o atajos de teclado para cambiar de planta**.
  - Los patos transitan entre niveles subiendo y bajando por **tramos de escaleras físicas industriales** con peldaños hazard, pasamanos dobles y un **colisionador de rampa invisible suave** que asegura una carrera continua a 14 m/s sin trabarse en las aristas.
  - Tramo 1 (Planta 1 -> Planta 2): en estribor, de $Y = 0\text{m}$ a $Y = 4.5\text{m}$.
  - Tramo 2 (Planta 2 -> Planta 3): en babor, de $Y = 4.5\text{m}$ a $Y = 9.0\text{m}$.

### Distribución de Botones Físicos en las 3 Plantas (`CockpitFloorButton`)

```
==================================================================================
PLANTA 3 (Y = 9.0m) - CABEZA DEL MECHA / CÁPSULA DEL CAPITÁN [3ª PERSONA AUTOMÁTICA]
   - Estructura Blindada de la Cabeza del Mecha (Sin ventanas: visión externa neural)
   - Pedestal Central de Mando del Capitán (X: 0.0, Y: 9.2, Z: +1.5) con anillo holo-disc
   - Consola Táctica de Mando (X: 0.0, Y: 9.45, Z: +3.8, Y < 0.6m)
   - [🚨 SPAWN] (X: 0.0, Y: 9.0, Z: -3.0) -> Conmutador Invasión Robots (Stomp)
   ▲ Escalera 2 que desciende hacia Planta 2 en babor (X: -7.2)
==================================================================================
PLANTA 2 (Y = 4.5m) - ESTACIÓN DE ARTILLERÍA Y ARSENAL [MÁXIMA VISIBILIDAD PANORÁMICA]
   - Gran ventanal frontal de 20m al 96% transparente y ventanas hacia armas
   - HUD Holográfico de Puntería proyector en parabrisas (Y = 6.8m)
   - [🎯 AIM PAD] (X: 0.0, Y: 4.54, Z: +3.5) -> Giróscopo de Puntería 2D centralizado frente al HUD
   - [🔫 GATLING] (X: -4.8, Y: 4.50, Z: +1.5) -> Ráfaga Gatling (Hold / Stomp) en ala babor
   - [🚀 MISSILES] (X: +4.8, Y: 4.50, Z: +1.5) -> Salva Micromisiles Guiados (Stomp) en ala estribor
   - Cubierta de tiro amplia y despejada (sin mesas ni elementos decorativos obstructivos)
   ▲ Escalera 1 que desciende hacia Planta 1 en estribor (X: +7.2)
==================================================================================
PLANTA 1 (Y = 0.0m) - ESTACIÓN DE CONDUCCIÓN Y NAVEGACIÓN [BÚNKER BLINDADO MILITAR]
   - Visibilidad Búnker Militar: Tronera militar estrecha (14m x 0.9m) a nivel de ojos (Y = 1.15m)
   - Muros gruesos de blindaje (El conductor es guiado por las órdenes del Capitán)
   - Consola de Navegación central con pantalla militar verde (X: 0.0, Z: +5.8)
   - [▲ FWD] (X: 0.0, Y: 0.0, Z: +3.8) -> Marcha hacia adelante
   - [▼ BWD] (X: 0.0, Y: 0.0, Z: -2.5) -> Marcha hacia atrás
   - [◄ LEFT] (X: -4.8, Y: 0.0, Z: +0.65) -> Giro sobre eje a la izquierda
   - [RIGHT ►] (X: +4.8, Y: 0.0, Z: +0.65) -> Giro sobre eje a la derecha
==================================================================================
```

| Nivel | Botón ID | Coordenadas Locales | Tipo | Función |
| :--- | :--- | :--- | :--- | :--- |
| **Planta 1** | **`FWD`** | `(0.0, 0.0, +3.8)` | `Hold` | Marcha hacia adelante del mecha |
| **Planta 1** | **`BWD`** | `(0.0, 0.0, -2.5)` | `Hold` | Marcha hacia atrás del mecha |
| **Planta 1** | **`LEFT`** | `(-4.8, 0.0, +0.65)` | `Hold` | Rotación sobre su eje a la izquierda |
| **Planta 1** | **`RIGHT`** | `(+4.8, 0.0, +0.65)` | `Hold` | Rotación sobre su eje a la derecha |
| **Planta 2** | **`AIM_PAD`** | `(0.0, 4.54, +3.5)` | `Hold` / `Stomp` | **Giroscopio de Puntería 2D**: Centralizado frente al HUD crosshair y parabrisas de 20m. Correr sobre la plataforma inclina analógicamente la mira y los gimbals de las armas (azimut $\pm 48^\circ$, elevación $-20^\circ$ a $+36^\circ$). **Culazo (Stomp)** ejecuta **Smart Lock-On** al enemigo más cercano. |
| **Planta 2** | **`GATLING`** | `(-4.8, 4.50, +1.5)` | `Hold` / `Stomp` | **Fuego continuo orientado** al pisar (12 bps) o **ráfaga masiva de 10 tiros** al dar culazo, alineado con el brazo ametralladora izquierdo |
| **Planta 2** | **`MISSILES`**| `(+4.8, 4.50, +1.5)` | `Stomp` | **Salva de 4 micromisiles guiados** lanzados desde el pod orientable, alineado con el hombro derecho de misiles (Cooldown 3.5s) |
| **Planta 3** | **`SPAWN`** | `(0.0, 9.0, -3.0)` | `Toggle` | Activa/desactiva la invasión de robots enemigos (requiere culazo en el Puente de Mando) |

---

## 4. 🧩 Arquitectura de Sistemas y Scripts

### A. Locomoción Física y Estabilidad del Mecha Colosal (50m)
- [`MechaController.cs`](file:///Assets/Scripts/MechaController.cs):
  - Rigidbody dinámico (masa 48,000 kg, altura 50.0m, pelvis a $Y = 21.0\text{m}$, collider $20\text{m} \times 50\text{m} \times 18\text{m}$).
  - Suspensión muelle-amortiguador con `RaycastNonAlloc` (`targetRideHeight = 21.0m`, `legRayLength = 28.0m`, `suspensionStrength = 1,900,000 N/m`, `suspensionDamping = 230,000 N·s/m`).
  - Fuerza de empuje: `movePower = 1,250,000 N`, `turnTorque = 950,000 N·m`, velocidad máxima calibrada a 22 m/s.
  - Centro de masas: `(0, 6.2m, 0.05m)` con tensor de inercia reforzado `(880000, 500000, 950000)`.
  - **Físicas de Caída y Robot Torpe (`IsAirborne`)**: Gravedad extra masiva de caída (`extraFallGravity = 17.5 m/s²`), amortiguación aérea mínima (`linearDamping = 0.05f`) y torsión oscilante cómica.
  - **Impacto de Aterrizaje Brutal (`HandleLandingImpact`)**: Landing squash elástico hasta 2.8m, congelación temporal de 65ms para impactos >14 m/s, trauma cuadrático de sacudida y sonido sub-grave en [`ProceduralAudio.PlayCrashLand()`](file:///Assets/Scripts/ProceduralAudio.cs).
  - Control de vuelco: si la inclinación supera 60°, entra en estado `isOverturned`.
  - Maniobra **¡PATALETA HIDRÁULICA! (`RecoverFromTumble`)**: Salto propulsor vertical de 28 m/s que endereza el mecha al hacer culazo o saltar cuando está volcado.
  - **Resistencia a Hot-Reload**: Método `EnsureBalanceModel()` invocado en `Awake()`, `OnEnable()` y `FixedUpdate()` para evitar pérdida de estado del modelo de péndulo invertido tras recompilaciones.
- [`ProceduralLeg.cs`](file:///Assets/Scripts/ProceduralLeg.cs) & [`MechBodyStabilizer.cs`](file:///Assets/Scripts/MechBodyStabilizer.cs):
  - Two-Bone IK analítico: caderas en $X = \pm 8.4\text{m}$, fémur 10.4m, tibia 9.6m (alcance 20.0m), `stepDistance = 6.0m`, `stepHeight = 2.2m`, `maxClimbableStepHeight = 5.2m`, botas blindadas *Hazard Stomper* de 6.4m x 3.6m.
  - Pataleo aéreo cómico en caída libre con desplazamiento en contrafase.

### B. Control del Pato y Físicas Multinivel
- [`DuckController.cs`](file:///Assets/Scripts/DuckController.cs):
  - Espacio local tridimensional de cabina expandido: `cockpitHalfX = 10.2m`, `cockpitHalfZ = 7.8m`, `cockpitMaxY = 16.0m`.
  - Detección continua de suelo y escalones mediante buffer prealocado `Physics.RaycastNonAlloc` con `stepSnapOffset = 0.85f`, permitiendo subir y bajar peldaños a 14 m/s con adherencia anti-deslizamiento.
  - **Físicas Estrictas Anti-Salto entre Plantas**:
    - Fuerza de salto calibrada a `jumpForce = 10.5m/s` (elevación ágil de ~1.5m para botones y pedestales, muy por debajo de la altura entre pisos de 4.5m).
    - Techo físico dinámico (`GetCeilingCenterY`): Clamping de techo para Planta 1 (3.6m) y Planta 2 (8.1m) que frena en seco cualquier velocidad ascendente si la cabeza impacta la estructura.
    - Detección de suelo por raycast con filtrado de planta: si el pato no se encuentra dentro de las cajas de escalera (`IsInStairwell1`, `IsInStairwell2`), se descarta cualquier superficie de plantas superiores.
    - Fallback de planta conservador: conserva la planta actual del pato y previene cualquier teletransporte indeseado hacia arriba.
  - Detección de planta en tiempo real (`CurrentFloorLevel = 1, 2 ó 3` y `CurrentFloorName`).
  - **Aislamiento Vertical de Botones**: Validación tridimensional estricta en `CockpitFloorButton` (`|Y_pato - Y_boton| < 1.6f`) para evitar activaciones accidentales entre plantas superpuestas.

### C. Sistema de Cámaras: Exclusividad del Capitán
- [`CockpitCameraController.cs`](file:///Assets/Scripts/CockpitCameraController.cs):
  - **Plantas 1 y 2 (Conductor y Artillero)**:
    - Cámara **ESTRICTAMENTE BLOQUEADA EN EL INTERIOR**.
    - Distancia máxima de zoom restringida a `maxInteriorDistance = 6.2f`.
    - Modo espectador / dron (`C`) desactivado.
    - Sistema Push-In anti-recorte con bounding boxes volumétricas adaptadas a la altura de cada planta.
  - **Planta 3 (Puente del Capitán)**:
    - En el suelo, escaleras y consola: cámara bloqueada en **vista interior** para máxima maniobrabilidad.
    - Al entrar en el **Círculo Marcado del Pedestal** (`Captain_Command_Pedestal`, $X=0, Z=+1.5, R=2.2\text{m}$): se desbloquea y activa la visión en **Tercera Persona Exterior** del Mecha colosal de 50 metros.
    - Zoom exterior con rueda de ratón hasta 160.0m y modo dron (`C`) habilitados exclusivamente dentro del círculo.
    - **Replegado Automático**: Al salir del círculo marcado o descender hacia Planta 2 o Planta 1, la cámara se repliega de forma inmediata y automática al interior de la cabina (`targetDistance = maxInteriorDistance`).

### D. Arsenal Orientable y Sistema de Puntería (Planta 2)
- [`GunnerAimPad.cs`](file:///Assets/Scripts/GunnerAimPad.cs):
  - Plataforma giroscópica analógica de puntería instalada en Planta 2 (`(+3.5m, 4.54m, +3.5m)`).
  - 4 pistones hidráulicos amortiguadores físicos que responden a la posición del pato.
  - Modulación suave de azimut ($\pm 48^\circ$) y elevación ($-20^\circ$ a $+36^\circ$) con retención de ángulo (*Aim Hold*).
  - Culazo ejecuta Smart Lock-On inmediato.
- **Brazo Izquierdo — Ametralladora Rotatoria Gatling (`Arm_Left_Gatling`)**:
  - Hombro en $X = -13.6\text{m}$. Gimbal articulado con 6 cañones cromados rotatorios y tambor inferior hazard.
  - [`GatlingTracer.cs`](file:///Assets/Scripts/GatlingTracer.cs): Proyectiles trazadores a 160 m/s sin GC allocations.
- **Hombro Derecho — Pod Lanzamisiles Orientable de 6 Tubos (`Missile_Pod`)**:
  - Hombro en $X = +13.6\text{m}$. Gimbal tridimensional orientable sincronizado con la mira del artillero.
  - [`MissileProjectile.cs`](file:///Assets/Scripts/MissileProjectile.cs): Micromisiles guiados con motor cohete (82 m/s), autoguiado dual y daño en área (AOE 9.5m).

### E. HUD Holográfico y UI Toolkit
- [`WindshieldHUDCrosshair.cs`](file:///Assets/Scripts/WindshieldHUDCrosshair.cs):
  - Proyectado sobre el parabrisas de Planta 2 a `baseHeightY = 6.8f` con telémetro, horizonte artificial y retícula de puntería dinámica.
- [`MechaHUD.cs`](file:///Assets/Scripts/MechaHUD.cs) & UI Toolkit (`MechaHUD.uxml`, `MechaHUD.uss`):
  - Badge de telemetría de planta en la barra superior con transiciones de color:
    - *Planta 1*: Verde esmeralda (`PLANTA 1: CONDUCTOR [Vista Interior]`).
    - *Planta 2*: Ámbar dorado (`PLANTA 2: ARTILLERO [Vista Interior]`).
    - *Planta 3*: Cian emisivo (`PLANTA 3: CAPITÁN [Vista Exterior 3ª Persona Activa]`).

### F. Herramientas de Editor y Generadores
- [`Assets/Scripts/Editor/MechaPrefabBuilder.cs`](file:///Assets/Scripts/Editor/MechaPrefabBuilder.cs):
  - Menú `Mecha > Build All Standard Prefabs`: Construye y serializa automáticamente todos los prefabs en `Assets/Prefabs/` (Mecha, Cockpit multinivel con escaleras, personajes, botones, consolas, enemigos).
- [`Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs`](file:///Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs):
  - Menú `Mecha > Generate Mega Proving Grounds`: Megaterreno procedural de 1200m x 1200m con pórtico gantry de salida elevado a **68m de luz libre** para el Mecha de 50m, pista de slalom, escalones, rampas, foso de vuelco, cañón de ácido de 440m y rascacielos urbanos de 95m a 165m.
- [`Assets/Scripts/Editor/AssembleMechaPrototype.cs`](file:///Assets/Scripts/Editor/AssembleMechaPrototype.cs):
  - Menú `Mecha > Assemble Complete Prototype`: Reensambla desde cero toda la escena con el Mecha de 50m, complejo de 3 plantas, escaleras, HUD y terreno.
- [`Assets/Scripts/AutoPlayTester.cs`](file:///Assets/Scripts/AutoPlayTester.cs):
  - Batería automatizada de **11 pruebas exhaustivas en modo Play** (Avance, Retroceso, Giros L/R, Gatling, Misiles, Aim Pad, Invasión Robots, Vuelco/Pataleta, UI Toolkit y Navegación Multinivel 3 Plantas con Exclusividad de Cámara).

---

## 5. ⚠️ Directrices Cruciales para Futuras Modificaciones

1. **Estructura Estándar de Prefabs**: Mantener y actualizar siempre los prefabs en `Assets/Prefabs/` (`Mecha`, `Cockpit`, `Characters`, `Props/Buttons`, `Props/Consoles`, `Enemies`).
2. **Prohibido Teletransporte entre Plantas**: La navegación vertical de los patos DEBE realizarse exclusivamente a pie o mediante saltos sobre las escaleras físicas.
3. **Exclusividad de Cámara del Capitán**: NUNCA permitir cámara en tercera persona exterior ni modo dron cuando el pato se encuentre en Planta 1 o Planta 2.
4. **Visibilidad en Cúpula**: NUNCA colocar cilindros, cajas u objetos opacos en el centro del parabrisas. Las consolas deben mantener un perfil bajo (`Y < 0.6m`).
5. **Compatibilidad hacia atrás**: Mantener siempre las propiedades públicas y aliases esperados por `AutoPlayTester.cs` (`LaserCooldown`, `IsGatlingFiring`, `GunnerAimYaw`, etc.).
6. **Espacio Local de Cabina**: Cualquier movimiento, fuerza o teletransporte del pato debe hacerse en **coordenadas locales de la cabina (`cockpitTransform`)** respetando `cockpitHalfX = 10.2m`, `cockpitHalfZ = 7.8m`, `cockpitMaxY = 16.0m`.
7. **Filosofía Zero External Assets y Cero GC Allocations**: Utilizar únicamente primitivas 3D procedurales optimizadas, shaders nativos de URP y síntesis acústica en runtime. No alojar memoria en bucles `Update` ni `FixedUpdate`.
