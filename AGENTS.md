# 🦆 MECHAQUACK - GUÍA DE ARQUITECTURA Y PROYECTO

> Archivo de contexto del proyecto para agentes de IA (**GEMINI.md** / **AGENTS.md**).
> Este documento resume la visión, arquitectura, jerarquías, sistemas mecánicos y convenciones técnicas para garantizar coherencia en futuras modificaciones.

---

## 1. 📖 Visión y Premisa del Juego

**MechaQuack** es un videojuego cómico y táctico en 3D desarrollado en **Unity 6 (6000.3.8f1)** con **Universal Render Pipeline (URP)**.

- **Premisa**: Un patito de goma pilota un Mecha bípede colosal gigante (~52m de altura) desde el interior de una espaciosa cabina física acristalada tipo puente de mando con cúpula panorámica 360° (22m x 17m).
- **Mecánica Central de Control**: El pato no dispone de mandos convencionales; para controlar la locomoción y el arsenal del mecha debe **correr, saltar y dar culazos/pisotones** directamente sobre botones físicos gigantes instalados en las plataformas de la cabina.
- **Filosofía Co-Op Multi-Estación**: La cabina está dividida en dos alas operativas independientes de 8.5m x 8.5m separadas por un pasillo central para permitir que en el futuro un segundo jugador (P2) asuma las armas mientras el primer jugador (P1) se encarga de la navegación.
- **Entorno**: Megaterreno post-apocalíptico de 1200m x 1200m con rascacielos derruidos, desniveles rocosos, un cañón de ácido tóxico corrosivo de 440m y oleadas de robots enemigos hostiles (`EnemyBot`).

---

## 2. 🛠️ Stack Tecnológico y Reglas del Proyecto

- **Motor**: Unity 6 LTS (`6000.3.8f1`) con Universal Render Pipeline (URP).
- **Estructura Estándar de Prefabs (`Assets/Prefabs/`)**: Todos los elementos del juego (Mecha, Cúpula, Pato, Botones, Consolas, Enemigos) están estructurados como prefabs reutilizables en carpetas estándar de Unity (`Prefabs/Mecha`, `Prefabs/Cockpit`, `Prefabs/Characters`, `Prefabs/Props/Buttons`, `Prefabs/Props/Consoles`, `Prefabs/Enemies`).
- **Filosofía "Zero External Assets"**: Todo el arte, geometría, efectos visuales y audio se generan **proceduralmente** mediante primitivas 3D optimizadas, shaders de URP y síntesis matemática de ondas en runtime. **No añadir dependencias binarias externas** a menos que el usuario lo solicite expresamente.
- **Interfaz (UI)**: Desarrollada con **UI Toolkit** (`UIDocument`, `MechaHUD.uxml`, `MechaHUD.uss`, y `MechaFontAsset.asset` con fuentes SDF).
- **Sistema de Entrada**: Unity New Input System (`com.unity.inputsystem`) soportando Teclado, Ratón y Gamepad.
- **Rendimiento y Memoria**: Cero allocations por frame en bucles críticos (`Update`, `FixedUpdate`). Uso estricto de buffers prealocados para físicas (`Physics.RaycastNonAlloc`).

---

## 3. 🕹️ Esquema de Controles y Distribución de Estaciones en Cabina

### Cabina del Mecha (Pato)
- **`W`, `A`, `S`, `D` / Flechas**: Mover al pato por el suelo de la cabina (movimiento relativo a la orientación de la cámara con fricción y resistencia a la inclinación, velocidad calibrada a 14 m/s).
- **`Espacio`**: Saltar entre las plataformas y botones físicos (`jumpForce = 15.5m/s`).
- **`E` / `Shift` / Clic Izquierdo (en el aire)**: **¡CULAZO! (Stomp)**. Pisotón en picado (`stompSpeed = 32m/s`) que activa los botones especiales de impacto y armas.
- **`Q` / Clic Derecho**: **¡CUAC!** Graznido cómico con efecto de sonido, aleteo y texto flotante.

- **Cámara Orbital Libre 360º con Anti-Recorte (Camera Collision Push-In)**:
  - **Arrastrar Clic (Izquierdo / Derecho)**: Orbitar 360º alrededor de la cabina / pato.
  - **Rueda del Ratón (Scroll)**: Zoom continuo desde el interior de la cabina (primer plano) hasta vista exterior completa del Mecha Titán de 52 m (hasta 160 m).
  - **`C`**: Alternar modos de cámara (Órbita libre 360º, Fija de cabina, Dron exterior).
  - **Sistema Anti-Recorte (Push-In)**: Si la cámara se aproxima a las paredes interiores de la cabina (mampara trasera, techo, cristales) o a edificios/terreno exteriores, se empuja suave y automáticamente hacia el pato (hasta 0.85m) para que el jugador nunca pierda visibilidad ni atraviese geometrías.

### Distribución de Botones Físicos en Cabina (`CockpitFloorButton`)

```
==================== PARABRISAS FRONTAL ====================
   [Ala Babor: P1 PILOTAJE]      |      [Ala Estribor: P2 ARTILLERÍA]
                                 |
         [▲ FWD]                 |         [🎯 AIM PAD / PUNTERÍA]
    (X: -4.5, Z: 4.5)            |            (X: +4.5, Z: 4.6)
                                 |
[◄ LEFT]        [RIGHT ►]        |    [🔫 GATLING]        [🚀 MISSILES]
(X: -7.2)       (X: -1.8)        |  (X: +2.6, Z: 2.5)   (X: +6.4, Z: 2.5)
                                 |
         [▼ BWD]                 |
    (X: -4.5, Z: 0.5)            |
                                 |
----------------- PASILLO CENTRAL Y OPERACIONES -----------------
                          [🚨 SPAWN]
                       (X: 0.0, Z: -2.5)
                                 |
                  [PUESTO MAQUINISTA ARTILLERO]
                       (X: 0.0, Z: -6.5)
```

| Estación | Botón ID | Coordenadas Locales | Tipo | Función |
| :--- | :--- | :--- | :--- | :--- |
| **P1 - Pilotaje** | **`FWD`** | `(-4.5, 0.20, +4.5)` | `Hold` | Marcha hacia adelante del mecha |
| **P1 - Pilotaje** | **`BWD`** | `(-4.5, 0.20, +0.5)` | `Hold` | Marcha hacia atrás del mecha |
| **P1 - Pilotaje** | **`LEFT`** | `(-7.2, 0.20, +2.5)` | `Hold` | Rotación sobre su eje a la izquierda |
| **P1 - Pilotaje** | **`RIGHT`** | `(-1.8, 0.20, +2.5)` | `Hold` | Rotación sobre su eje a la derecha |
| **P2 - Artillería**| **`AIM_PAD`**| `(+4.5, 0.04, +4.6)` | `Hold` / `Stomp` | **Giroscopio de Puntería 2D**: Correr sobre la plataforma inclina analógicamente la mira y los gimbals de las armas (azimut $\pm 48^\circ$, elevación $-20^\circ$ a $+36^\circ$). **Culazo (Stomp)** ejecuta **Smart Lock-On** al enemigo más cercano (o recentra mira a $0^\circ, 0^\circ$). Un culazo adicional sobre blanco fijado dispara salva de misiles guiados inmediata. |
| **P2 - Artillería**| **`GATLING`**| `(+2.6, 0.20, +2.5)` | `Hold` / `Stomp` | **Fuego continuo orientado** al pisar (12 bps) o **ráfaga masiva de 10 tiros** al dar culazo disparados hacia el vector apuntado por el artillero |
| **P2 - Artillería**| **`MISSILES`**|`(+6.4, 0.20, +2.5)` | `Stomp` | **Salva de 4 micromisiles guiados** lanzados desde el pod orientable hacia el objetivo apuntado (Cooldown 3.5s) |
| **Operaciones** | **`SPAWN`** | `( 0.0, 0.20, -2.5)` | `Toggle` | Activa/desactiva la invasión de robots enemigos (requiere culazo) |

---

## 4. 🧩 Arquitectura de Sistemas y Scripts

### A. Locomoción Física y Estabilidad del Mecha Colosal (~52m)
- [`MechaController.cs`](file:///Assets/Scripts/MechaController.cs):
  - Rigidbody dinámico (masa 44,000 kg, altura ~52m, envergadura 35m).
  - Suspensión independiente muelle-amortiguador en ambas piernas calculada con `RaycastNonAlloc` (`targetRideHeight = 21.2m`, `legRayLength = 28.0m` [recorrido amortiguado para desniveles], `suspensionStrength = 1,800,000 N/m`, `suspensionDamping = 220,000 N·s/m`).
  - Fuerza de empuje: `movePower = 1,200,000 N`, `turnTorque = 900,000 N·m`, `maxLinearVelocity = 22 m/s`.
  - Centro de masas elevado a `(0, 6.4m, 0)`.
  - **Físicas de Caída y Robot Torpe (`IsAirborne`)**:
    - Al caer por desniveles/escalones, el rozamiento del aire cae drásticamente (`linearDamping = 0.05f`), eliminando el efecto de "planeo flotante".
    - Aceleración pesada descendente: gravedad extra masiva (`extraFallGravity = 17.5 m/s²`, aceleración total $\approx 27.3\text{ m/s}^2$).
    - Torsión cómica de braceo aéreo (`AddTorque` oscilante) que desestabiliza el chasis durante la caída.
  - **Impacto de Aterrizaje Brutal (`HandleLandingImpact`)**:
    - **Aplastamiento Mecánico (Landing Squash)**: Compresión de rodillas/chasis hasta 2.8m con retorno elástico.
    - **Hit-Stop / Freeze Frame**: Congelación temporal de 65ms a escala de tiempo 0.04 para impactos superiores a 14 m/s.
    - **Trauma y Sacudida de Pantalla**: Trauma cuadrático proporcional al impacto vertical.
    - **Sonido de Aterrizaje Procedural**: Sub-grave sawtooth (140Hz a 32Hz) + crujido metálico y graznido cómico en [`ProceduralAudio.PlayCrashLand()`](file:///Assets/Scripts/ProceduralAudio.cs).
    - **Efectos Cómicos**: Vóxeles de polvo en ambos pies ([`VoxelExplosion.cs`](file:///Assets/Scripts/VoxelExplosion.cs)), expulsión por inercia del patito en cabina ([`DuckController.ApplyExternalNudge`](file:///Assets/Scripts/DuckController.cs)) y rótulos retro ("💥 ¡¡CATAPLUM!! ¡ATERRIZAJE FORZOSO!").
  - Control de vuelco: si la inclinación supera los 60°, entra en estado `isOverturned`.
  - Maniobra de emergencia **¡PATALETA HIDRÁULICA! (`RecoverFromTumble`)**: Salto propulsor con impulso vertical de 28 m/s que endereza el mecha al hacer un culazo o salto cuando está volcado.
- [`ProceduralLeg.cs`](file:///Assets/Scripts/ProceduralLeg.cs) & [`MechBodyStabilizer.cs`](file:///Assets/Scripts/MechBodyStabilizer.cs):
  - Sistema de locomoción procedural con IK analítico (Two-Bone IK) adaptado al mecha de 52m: caderas en $X = \pm 8.4\text{m}$, longitud de fémur 10.4m, tibia 9.6m (alcance 20m), `stepDistance = 6.4m`, `stepHeight = 2.2m`, `maxClimbableStepHeight = 5.2m`, botas blindadas *Hazard Stomper* con suela de 6.4m x 3.6m.
  - **Pataleo Aéreo Cómico**: En el aire, las piernas cuelgan bajo las caderas y patalean cómicamente en contrafase ($\sin(t) \cdot 1.5\text{m}$ en Y, $\cos(t) \cdot 2.2\text{m}$ en Z) en lugar de congelarse en el cielo. Inclinación del torso aerodinámica en caída libre.

### B. Control del Pato y Cúpula Panorámica de Máxima Visibilidad
- [`DuckController.cs`](file:///Assets/Scripts/DuckController.cs):
  - Físicas en espacio local de la cabina (`transform.InverseTransformDirection`).
  - Límites de cabina expandidos: `cockpitHalfX = 10.2m`, `cockpitHalfZ = 7.8m` (puente de 22m x 17m).
  - Tolerancia a la inclinación: `tiltToleranceAngle = 12°`. El pato puede caminar y contrarrestar la gravedad de la cabina incluso cuando el mecha se inclina hasta 75°.
- **Cúpula Panorámica 360° (Glass Canopy Dome)**:
  - Parabrisas frontal panorámico ultra-transparente (`Windshield_Front`, 20.4m x 8.2m) con material URP transparente `M_CockpitDomeGlass.mat` (96% de transmisión óptica).
  - Techo acristalado (`Canopy_Ceiling_Glass`, 19.6m x 15.0m) y ventanales laterales envolventes (`Side_Glass_L`, `Side_Glass_R`) para visión cenital y lateral completa de los rascacielos y el arsenal de los brazos.
  - Estructura estilizada de jaula antivuelco tubular (`RollBar_Front_Brow`, `RollBar_Mid_Hoop`, pilares A y B de 0.4m) que no interrumpe la visualización.
  - Consolas de escritorio de perfil bajo (`Y = 0.45m`) con pantallas MFD anguladas a -25° situadas bajo el nivel de los ojos del pato para visión limpia del horizonte.
  - **Sin elementos opacos centrales**: Cualquier reactor de energía se ubica en el abdomen/pelvis inferior, manteniendo el parabrisas 100% despejado.
- [`MechaMachinistGunner.cs`](file:///Assets/Scripts/MechaMachinistGunner.cs):
  - Puesto elevado en la cabina (`Z = -6.5m`) con patito copiloto maquinista: gorra militar, gafas de visión táctica cian, consolas con palancas de tiro (*flight sticks*) y pantalla de radar holográfica verde.

### C. Arsenal Orientable y Sistema de Puntería de Artillería (Gunner Aiming System)
- [`GunnerAimPad.cs`](file:///Assets/Scripts/GunnerAimPad.cs):
  - Plataforma giroscópica analógica de puntería instalada en el Ala de Artillería (coordenadas locales `(+4.5m, 0.04m, +4.6m)`).
  - Base con anillo de franjas hazard, rótula central cromada y 4 pistones hidráulicos amortiguadores que se comprimen físicamente en tiempo real según la posición del pato.
  - Al caminar hacia adelante/atrás o izquierda/derecha, modula suavemente el azimut (`gunnerAimYaw`, $\pm 48^\circ$) y elevación (`gunnerAimPitch`, $-20^\circ$ a $+36^\circ$).
  - **Retención de Ángulo (Aim Hold)**: Al bajarse de la plataforma, la mira y los gimbals de las armas se mantienen apuntados en el ángulo deseado para permitir correr libremente hacia los botones de disparo.
  - **Smart Lock-On con Culazo (Stomp)**: Al dar un culazo sobre la plataforma, escanea los robots enemigos en el cono frontal y clava instantáneamente el azimut y elevación en el blanco prioritario. Un culazo posterior mientras el blanco está fijado lanza directamente una salva de misiles guiados. Si no hay enemigos al frente, recentra la mira a $(0^\circ, 0^\circ)$.
- **Brazo Izquierdo — Ametralladora Rotatoria Gatling con Gimbal Orientable (`Arm_Left_Gatling`)**:
  - Hombro en $X = -13.6\text{m}$. Gimbal articulado (`gatlingGimbalTransform`) que se orienta dinámicamente en pitch y yaw hacia el objetivo apuntado por el artillero.
  - Rotor giratorio de 6 cañones cromados (`Gatling_Rotor`), tambor de munición inferior con banda hazard y fogonazo dinámico.
  - [`GatlingTracer.cs`](file:///Assets/Scripts/GatlingTracer.cs): Proyectiles trazadores a 160 m/s con `TrailRenderer`, luz puntual y detección de impacto instantánea disparados con precisión a lo largo del vector de puntería `GetGunnerAimDirection()`.
- **Hombro Derecho — Pod Lanzamisiles Orientable de 6 Tubos (`Missile_Pod`)**:
  - Hombro en $X = +13.6\text{m}$. Gimbal articulado (`missilePodGimbalTransform`) que gira en 3D para alinear los 6 tubos con la dirección de tiro del artillero.
  - [`MissileProjectile.cs`](file:///Assets/Scripts/MissileProjectile.cs): Micromisiles guiados que despegan disparados hacia el vector frontal apuntado por el artillero (`GetGunnerAimDirection()`), ignoran colisiones con el propio chasis del Mecha en el lanzamiento, aceleran a 82 m/s y cuentan con autoguiado dual dinámico: rastrean activamente al objetivo fijado por el HUD (`CurrentLockedTarget`) o el punto balístico continuo del visor (`GetGunnerAimTargetPoint(280f)`), impactando con detonación masiva en área (AOE 9.5m).

### D. HUD Holográfico Dinámico en el Parabrisas Panorámico
- [`WindshieldHUDCrosshair.cs`](file:///Assets/Scripts/WindshieldHUDCrosshair.cs):
  - Proyectado sobre la superficie interior del cristal panorámico de la cúpula, desplazándose dinámicamente a lo largo de la curvatura focal (~8.0m) según el azimut y elevación apuntados desde la estación de artillería.
  - **Retícula abierta**: Punto central de precisión con 4 marcas cardinales y espacio abierto central.
  - **Targeting Brackets (`⌜ ⌝ ⌞ ⌟`)**: Cuatro esquinas tácticas que se contraen y pasan de cyan a rojo al detectar robots enemigos (*Lock-On* hasta 320m) a lo largo del rayo apuntado por el artillero.
  - **Telémetro y Telemetría Angular (`RNG: XXm | AZ: ±XX.X° EL: ±XX.X°`)**: Mide la distancia continua al objetivo o al terreno y muestra en tiempo real los ángulos balísticos de tiro.
  - **Horizonte artificial**: Dos alas laterales que giran contrarrestando el roll del mecha.
  - **Estado de armas**: Telemetría en tiempo real (`[GATLING] LISTA/¡FUEGO!` y `[MISILES] 4/4/RECARGA`).
  - **Recoil Bloom**: Expansión dinámica de la mirilla al disparar con amortiguación suave.

### E. Audio y Efectos Procedurales
- [`ProceduralAudio.cs`](file:///Assets/Scripts/ProceduralAudio.cs):
  - Sintetizador de audio en tiempo de ejecución (`AudioClip.Create`).
  - Genera: graznidos armónicos, tableteo percusivo de ametralladora Gatling, encendido y silbido de motor de misiles, pisotones, clics de relé, explosiones y alarmas.
  - Incluye micro-variación de tono (*pitch jitter* ±5%) para evitar fatiga auditiva.
- [`VoxelExplosion.cs`](file:///Assets/Scripts/VoxelExplosion.cs) & [`FloatingText.cs`](file:///Assets/Scripts/FloatingText.cs):
  - Desprendimiento de escombros de vóxeles y tipografía retro flotante para impactos y eventos cómicos.

### F. Herramientas de Editor y Generadores
- [`Assets/Scripts/Editor/MechaPrefabBuilder.cs`](file:///Assets/Scripts/Editor/MechaPrefabBuilder.cs):
  - Menú `Mecha > Build All Standard Prefabs` / `PatoJuego > Build Standard Prefabs`: Construye y serializa automáticamente todos los prefabs del proyecto en `Assets/Prefabs/` (Personajes, Cúpula, Botones, Consolas, Enemigos y Mecha).
- [`Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs`](file:///Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs):
  - Menú `Mecha > Generate Mega Proving Grounds (Terreno Extendido)` / `PatoJuego > Generate Mega Proving Grounds`: Genera el megaterreno de pruebas procedural de 1200m x 1200m con 8 sectores especializados de locomoción escalados a 2x:
    1. **Sector 0 (Centro)**: Plaza de calibración (160m x 160m) con cuadrícula métrica y gantry de salida (92m luz x 52m altura).
    2. **Sector 1 (Norte)**: Pista de alta velocidad (600m de longitud, 68m ancho) con arcos de telemetría a cada 100m, bandas sonoras de suspensión progresivas (0.36m a 0.84m) y circuito de slalom con 6 balizas luminosas para evaluar el torque de giro.
    3. **Sector 2 (Este)**: Circuito de escalones y adaptación: escalera progresiva de alturas (0.50m, 1.00m, 1.60m, 2.20m, 2.80m, 3.20m y tope de 3.90m), gran escalinata monumental a +16.8m, carril de peralte asimétrico (+1.70m) y zanjas de zancada (3.0m y 4.8m).
    4. **Sector 3 (Sureste)**: Colina de pendientes graduadas con rampas medidas de 64m de longitud a 12°, 24°, 34°, 44° y 56°, además de carretera de montaña en curvas ascendiendo a la cima panorámica (+56m).
    5. **Sector 4 (Sur)**: Foso de vuelco (150m x 150m x 9m profundidad) con cuña de balanceo a 65° (>60° límite de vuelco) para ensayar la **Pataleta Hidráulica**, foso de contención y rampa suave de reincorporación al Hub.
    6. **Sector 5 (Oeste)**: Badlands y campo de rocas procedural (360m x 300m) con 75+ peñascos deterministas a escala 2x y canal "Rock Crawler" de 8.4m para testear la respuesta de suspensión y normal de superficie de los pies.
    7. **Sector 6 (Noroeste)**: El Gran Cañón de Ácido con fosa de 440m de longitud y 124m de ancho (`AcidHazard`), islas transitables y 3 puentes gigantes: Autopista blindada (48m ancho), Puente fracturado (36m ancho) y Pasarela Razor (26m ancho, inclinación sin barandilla).
    8. **Sector 7 (Noreste)**: Distrito urbano de ruinas y combate (360m x 360m) con rascacielos derruidos de 60m a 150m, autopista elevada de francotirador (+15m) y plaza central para la invasión de `EnemyBot`.
- [`Assets/Scripts/Editor/AssembleMechaPrototype.cs`](file:///Assets/Scripts/Editor/AssembleMechaPrototype.cs):
  - Menú `Mecha > Assemble Complete Prototype` / `PatoJuego > Assemble Full Prototype`: Reensambla desde cero toda la escena incorporando el Megaterreno de 1200m, el Mecha Titán de ~52m con cúpula panorámica de máxima visibilidad, estaciones separadas P1/P2, HUD, EnemySpawner vinculado al prefab y serialización como Prefab estándar.
- [`Assets/Scripts/AutoPlayTester.cs`](file:///Assets/Scripts/AutoPlayTester.cs):
  - Batería automatizada de 10 pruebas en modo Play para validar dinámicamente: avance (+8.5m), retroceso (-5.9m), giros (±5.8°), Gatling, Misiles, **Sistema de Puntería de Artillería (Aim Pad y gimbals orientables)**, conmutador de invasión de robots, vuelco (>60°), Pataleta Hidráulica y UI Toolkit modal de fin de partida.

---

## 5. ⚠️ Directrices Cruciales para Futuras Modificaciones

1. **Estructura Estándar de Prefabs**: Mantener y actualizar siempre los prefabs en `Assets/Prefabs/` (`Mecha`, `Cockpit`, `Characters`, `Props/Buttons`, `Props/Consoles`, `Enemies`).
2. **Visibilidad en Cúpula**: NUNCA colocar cilindros, cajas u objetos opacos en el centro del parabrisas. Las consolas deben mantener un perfil bajo (`Y < 0.6m`) con pantallas compactas que no superen `Y = 1.0m`.
3. **Compatibilidad hacia atrás**: Mantener siempre las propiedades públicas y aliases esperados por `AutoPlayTester.cs` (`LaserCooldown`, `IsGatlingFiring`, etc.) para no romper los tests automatizados.
4. **Espacio Local de Cabina**: Cualquier movimiento, fuerza o teletransporte del pato debe hacerse en **coordenadas locales de la cabina (`cockpitTransform`)** dentro de los límites `cockpitHalfX = 10.2m`, `cockpitHalfZ = 7.8m`.
5. **Separación de Responsabilidades Co-Op**: Respetar la división física entre la Estación de Pilotaje (P1 en babor) y la Estación de Artillería (P2 en estribor). Cualquier botón adicional de armas debe ubicarse en estribor; cualquier control de movilidad o navegación en babor.
6. **Armamento Actual**: Las armas activas del mecha son la **Ametralladora Gatling** en el brazo izquierdo ($X = -13.6\text{m}$) y el **Lanzamisiles** en el hombro derecho ($X = +13.6\text{m}$).
