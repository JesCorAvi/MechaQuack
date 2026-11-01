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

### Cámara Orbital Libre 360º
- **Arrastrar Clic (Izquierdo / Derecho)**: Orbitar 360º alrededor de la cabina / pato.
- **Rueda del Ratón (Scroll)**: Zoom continuo desde el interior de la cabina (primer plano) hasta vista exterior completa del Mecha Titán de 52 m (hasta 160 m).
- **`C`**: Alternar modos de cámara (Órbita libre 360º, Fija de cabina, Dron exterior).

### Distribución de Botones Físicos en Cabina (`CockpitFloorButton`)

```
==================== PARABRISAS FRONTAL ====================
   [Ala Babor: P1 PILOTAJE]      |      [Ala Estribor: P2 ARTILLERÍA]
                                 |
         [▲ FWD]                 |         [🔫 GATLING]   [🚀 MISSILES]
    (X: -4.5, Z: 4.5)            |       (X: +2.6, Z: 2.5) (X: +6.4, Z: 2.5)
                                 |
[◄ LEFT]        [RIGHT ►]        |
(X: -7.2)       (X: -1.8)        |
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
| **P2 - Artillería**| **`GATLING`**| `(+2.6, 0.20, +2.5)` | `Hold` / `Stomp` | **Fuego continuo** al pisar (12 bps) o **ráfaga masiva de 10 tiros** al dar culazo |
| **P2 - Artillería**| **`MISSILES`**|`(+6.4, 0.20, +2.5)` | `Stomp` | **Salva de 4 micromisiles guiados** con culazo (Cooldown 3.5s) |
| **Operaciones** | **`SPAWN`** | `( 0.0, 0.20, -2.5)` | `Toggle` | Activa/desactiva la invasión de robots enemigos (requiere culazo) |

---

## 4. 🧩 Arquitectura de Sistemas y Scripts

### A. Locomoción Física y Estabilidad del Mecha Colosal (~52m)
- [`MechaController.cs`](file:///Assets/Scripts/MechaController.cs):
  - Rigidbody dinámico (masa 44,000 kg, altura ~52m, envergadura 35m).
  - Suspensión independiente muelle-amortiguador en ambas piernas calculada con `RaycastNonAlloc` (`targetRideHeight = 21.2m`, `legRayLength = 30.0m`, `suspensionStrength = 1,800,000 N/m`, `suspensionDamping = 220,000 N·s/m`).
  - Fuerza de empuje: `movePower = 1,200,000 N`, `turnTorque = 900,000 N·m`, `maxLinearVelocity = 22 m/s`.
  - Centro de masas elevado a `(0, 6.4m, 0)`.
  - Control de vuelco: si la inclinación supera los 60°, entra en estado `isOverturned`.
  - Maniobra de emergencia **¡PATALETA HIDRÁULICA! (`RecoverFromTumble`)**: Salto propulsor con impulso vertical de 28 m/s que endereza el mecha al hacer un culazo o salto cuando está volcado.
  - Gestión de Hit-Stop (`TriggerHitStop`) y sacudida cuadrática de cámara basada en trauma (`shakeIntensity`).
- [`ProceduralLeg.cs`](file:///Assets/Scripts/ProceduralLeg.cs) & [`MechBodyStabilizer.cs`](file:///Assets/Scripts/MechBodyStabilizer.cs):
  - Sistema de locomoción procedural con IK analítico (Two-Bone IK) adaptado al mecha de 52m: caderas en $X = \pm 8.4\text{m}$, longitud de fémur 10.4m, tibia 9.6m (alcance 20m), `stepDistance = 6.4m`, `stepHeight = 2.2m`, `maxClimbableStepHeight = 5.2m`, botas blindadas *Hazard Stomper* con suela de 6.4m x 3.6m.

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

### C. Arsenal del Mecha Militar Titán
- **Brazo Izquierdo — Ametralladora Rotatoria Gatling (`Arm_Left_Gatling`)**:
  - Rotor giratorio de 6 cañones cromados (`Gatling_Rotor`), tambor de munición inferior con banda hazard y conducto flexible de munición hacia el torso (hombro en $X = -13.6\text{m}$).
  - [`GatlingTracer.cs`](file:///Assets/Scripts/GatlingTracer.cs): Proyectiles trazadores a 160 m/s con `TrailRenderer`, luz puntual y detección de impacto instantánea contra `EnemyBot`.
- **Hombro Derecho — Pod Lanzamisiles de 6 Tubos (`Arm_Right_MissilePod` / `Missile_Pod`)**:
  - Pod rectangular blindado verde oliva con 6 tubos y cabezas ojivales cian luminosas (en 2 filas de 3, hombro en $X = +13.6\text{m}$).
  - [`MissileProjectile.cs`](file:///Assets/Scripts/MissileProjectile.cs): Micromisiles que aceleran de 22 a 65 m/s, emiten estela de humo, cuentan con autoguía hacia el enemigo apuntado por el visor y detonan con daño en área (AOE 9.5m).
- **Brazo Derecho — Manipulador de Combate**:
  - Brazo articulado con mano robótica militar de 4 dedos, nudillos cromados y pulgar mecánico.

### D. HUD Holográfico en el Parabrisas
- [`WindshieldHUDCrosshair.cs`](file:///Assets/Scripts/WindshieldHUDCrosshair.cs):
  - Proyectado sobre la superficie interior del cristal panorámico en coordenadas locales `(0, 4.4m, 8.0m)`.
  - **Retícula abierta**: Punto central de precisión con 4 marcas cardinales y espacio abierto central.
  - **Targeting Brackets (`⌜ ⌝ ⌞ ⌟`)**: Cuatro esquinas tácticas que se contraen y pasan de cyan a rojo al detectar robots enemigos (*Lock-On* hasta 260m).
  - **Telémetro dinámico (`RNG: XXm`)**: Mide la distancia continua al objetivo o al terreno mediante `SphereCast`/`Raycast`.
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
  - Batería automatizada de 9 pruebas en modo Play para validar dinámicamente: avance (+8.5m), retroceso (-5.9m), giros (±5.8°), Gatling, Misiles, conmutador de invasión de robots, vuelco (>60°), Pataleta Hidráulica y UI Toolkit modal de fin de partida.

---

## 5. ⚠️ Directrices Cruciales para Futuras Modificaciones

1. **Estructura Estándar de Prefabs**: Mantener y actualizar siempre los prefabs en `Assets/Prefabs/` (`Mecha`, `Cockpit`, `Characters`, `Props/Buttons`, `Props/Consoles`, `Enemies`).
2. **Visibilidad en Cúpula**: NUNCA colocar cilindros, cajas u objetos opacos en el centro del parabrisas. Las consolas deben mantener un perfil bajo (`Y < 0.6m`) con pantallas compactas que no superen `Y = 1.0m`.
3. **Compatibilidad hacia atrás**: Mantener siempre las propiedades públicas y aliases esperados por `AutoPlayTester.cs` (`LaserCooldown`, `IsGatlingFiring`, etc.) para no romper los tests automatizados.
4. **Espacio Local de Cabina**: Cualquier movimiento, fuerza o teletransporte del pato debe hacerse en **coordenadas locales de la cabina (`cockpitTransform`)** dentro de los límites `cockpitHalfX = 10.2m`, `cockpitHalfZ = 7.8m`.
5. **Separación de Responsabilidades Co-Op**: Respetar la división física entre la Estación de Pilotaje (P1 en babor) y la Estación de Artillería (P2 en estribor). Cualquier botón adicional de armas debe ubicarse en estribor; cualquier control de movilidad o navegación en babor.
6. **Armamento Actual**: Las armas activas del mecha son la **Ametralladora Gatling** en el brazo izquierdo ($X = -13.6\text{m}$) y el **Lanzamisiles** en el hombro derecho ($X = +13.6\text{m}$).
