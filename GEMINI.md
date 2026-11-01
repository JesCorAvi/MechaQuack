# 🦆 MECHAQUACK - GUÍA DE ARQUITECTURA Y PROYECTO

> Archivo de contexto del proyecto para agentes de IA (**GEMINI.md** / **AGENTS.md**).
> Este documento resume la visión, arquitectura, jerarquías, sistemas mecánicos y convenciones técnicas para garantizar coherencia en futuras modificaciones.

---

## 1. 📖 Visión y Premisa del Juego

**MechaQuack** es un videojuego cómico y táctico en 3D desarrollado en **Unity 6 (6000.3.8f1)** con **Universal Render Pipeline (URP)**.

- **Premisa**: Un patito de goma pilota un Mecha bípede gigante desde el interior de una cabina física acristalada.
- **Mecánica Central de Control**: El pato no dispone de mandos convencionales; para controlar la locomoción y el arsenal del mecha debe **correr, saltar y dar culazos/pisotones** directamente sobre botones físicos gigantes instalados en el suelo de la cabina.
- **Entorno**: Terreno post-apocalíptico con rascacielos derruidos, desniveles rocosos, un río de ácido tóxico corrosivo y oleadas de robots enemigos hostiles (`EnemyBot`).

---

## 2. 🛠️ Stack Tecnológico y Reglas del Proyecto

- **Motor**: Unity 6 LTS (`6000.3.8f1`) con Universal Render Pipeline (URP).
- **Filosofía "Zero External Assets"**: Todo el arte, geometría, efectos visuales y audio se generan **proceduralmente** mediante primitivas 3D optimizadas, shaders de URP y síntesis matemática de ondas en runtime. **No añadir dependencias binarias externas** a menos que el usuario lo solicite expresamente.
- **Interfaz (UI)**: Desarrollada con **UI Toolkit** (`UIDocument`, `MechaHUD.uxml`, `MechaHUD.uss`, y `MechaFontAsset.asset` con fuentes SDF).
- **Sistema de Entrada**: Unity New Input System (`com.unity.inputsystem`) soportando Teclado, Ratón y Gamepad.
- **Rendimiento y Memoria**: Cero allocations por frame en bucles críticos (`Update`, `FixedUpdate`). Uso estricto de buffers prealocados para físicas (`Physics.RaycastNonAlloc`).

---

## 3. 🕹️ Esquema de Controles

### Cabina del Mecha (Pato)
- **`W`, `A`, `S`, `D` / Flechas**: Mover al pato por el suelo de la cabina (movimiento relativo a la orientación de la cámara con fricción y resistencia a la inclinación).
- **`Espacio`**: Saltar entre los botones físicos.
- **`E` / `Shift` / Clic Izquierdo (en el aire)**: **¡CULAZO! (Stomp)**. Pisotón en picado que activa los botones especiales de impacto y armas.
- **`Q` / Clic Derecho**: **¡CUAC!** Graznido cómico con efecto de sonido, aleteo y texto flotante.

### Cámara Orbital Libre 360º
- **Arrastrar Clic (Izquierdo / Derecho)**: Orbitar 360º alrededor de la cabina / pato.
- **Rueda del Ratón (Scroll)**: Zoom continuo desde el interior de la cabina (primer plano) hasta vista exterior completa del Mecha (hasta 42 m).
- **`C`**: Alternar modos de cámara (Órbita libre 360º, Fija de cabina, Dron exterior).

### Botones Físicos del Suelo de la Cabina (`CockpitFloorButton`)
| Botón ID | Etiqueta | Tipo | Función |
| :--- | :--- | :--- | :--- |
| **`FWD`** | `▲ AVANZAR` | `Hold` | Marcha hacia adelante del mecha |
| **`BWD`** | `▼ ATRÁS` | `Hold` | Marcha hacia atrás del mecha |
| **`LEFT`** | `◄ GIRO IZQ` | `Hold` | Rotación sobre su eje a la izquierda |
| **`RIGHT`** | `GIRO DER ►` | `Hold` | Rotación sobre su eje a la derecha |
| **`GATLING`** | `🔫 GATLING` | `Hold` / `Stomp` | **Fuego continuo** al pisar (12 bps) o **ráfaga masiva de 10 tiros** al dar culazo |
| **`MISSILES`** | `🚀 MISILES` | `Stomp` | **Salva de 4 micromisiles guiados** con culazo (Cooldown 3.5s) |
| **`SPAWN`** | `🚨 ROBOTS` | `Toggle` | Activa/desactiva la invasión de robots enemigos |

---

## 4. 🧩 Arquitectura de Sistemas y Scripts

### A. Locomoción Física y Estabilidad del Mecha
- [`MechaController.cs`](file:///Assets/Scripts/MechaController.cs):
  - Rigidbody dinámico (masa 8000 kg, centro de masa elevado para balanceo realista).
  - Suspensión independiente muelle-amortiguador en ambas piernas calculada con `RaycastNonAlloc`.
  - Control de vuelco: si la inclinación supera los 60°, entra en estado `isOverturned`.
  - Maniobra de emergencia **¡PATALETA HIDRÁULICA! (`RecoverFromTumble`)**: Salto propulsor que endereza el mecha al hacer un culazo cuando está volcado.
  - Gestión de Hit-Stop (`TriggerHitStop`) y sacudida cuadrática de cámara basada en trauma (`shakeIntensity`).
- [`ProceduralLeg.cs`](file:///Assets/Scripts/ProceduralLeg.cs) & [`MechBodyStabilizer.cs`](file:///Assets/Scripts/MechBodyStabilizer.cs):
  - Sistema de locomoción procedural sin animaciones fijas: predicción de pisada con *home targets*, arco de paso con curvas polinómicas/`AnimationCurve`, marcha alternada bípede y adaptación a rocas/escalones.

### B. Control del Pato en Cabina
- [`DuckController.cs`](file:///Assets/Scripts/DuckController.cs):
  - Físicas en espacio local de la cabina (`transform.InverseTransformDirection`).
  - **Tolerancia a la inclinación**: `tiltToleranceAngle = 12°`. El pato puede caminar y contrarrestar la gravedad de la cabina incluso cuando el mecha se inclina hasta 75°.
  - Detección de impacto en suelo con pisotón aéreo (`TriggerAirStomp`).

### C. Arsenal del Mecha
- **Brazo Izquierdo — Ametralladora Rotatoria Gatling (`Arm_Left_Gatling`)**:
  - Rotor giratorio de 6 cañones (`Gatling_Rotor`), tambor de munición inferior y luz de fogonazo.
  - [`GatlingTracer.cs`](file:///Assets/Scripts/GatlingTracer.cs): Proyectiles trazadores a 160 m/s con `TrailRenderer`, luz puntual y detección de impacto instantánea contra `EnemyBot`.
- **Brazo Derecho — Pod Lanzamisiles de 4 Tubos (`Arm_Right_MissilePod`)**:
  - Pod rectangular blindado con 4 tubos en matriz 2x2 y ojivas visibles.
  - [`MissileProjectile.cs`](file:///Assets/Scripts/MissileProjectile.cs): Micromisiles que aceleran de 22 a 65 m/s, emiten estela de humo, cuentan con autoguía hacia el enemigo apuntado por el visor y detonan con daño en área (AOE 9.5m).

### D. HUD Holográfico en el Parabrisas
- [`WindshieldHUDCrosshair.cs`](file:///Assets/Scripts/WindshieldHUDCrosshair.cs):
  - Proyectado sobre la superficie interior del parabrisas (sustituye cualquier disco o cilindro azul sólido).
  - **Retícula abierta**: Punto central de precisión con 4 marcas cardinales y espacio abierto central.
  - **Targeting Brackets (`⌜ ⌝ ⌞ ⌟`)**: Cuatro esquinas tácticas que se contraen y pasan de cyan a rojo al detectar robots enemigos (*Lock-On*).
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

### F. Herramientas de Editor y Automatización
- [`Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs`](file:///Assets/Scripts/Editor/MechaProvingGroundsGenerator.cs):
  - Menú `Mecha > Generate Mega Proving Grounds (Terreno Extendido)` / `PatoJuego > Generate Mega Proving Grounds`: Genera el megaterreno de pruebas procedural de 600m x 600m con 8 sectores especializados de locomoción:
    1. **Sector 0 (Centro)**: Plaza de calibración (72m x 72m) con cuadrícula métrica y gantry de salida.
    2. **Sector 1 (Norte)**: Pista de alta velocidad (300m) con arcos de telemetría a cada 50m, bandas sonoras de suspensión progresivas (0.18m a 0.42m) y circuito de slalom con 6 balizas luminosas para evaluar el torque de giro.
    3. **Sector 2 (Este)**: Circuito de escalones y adaptación: escalera progresiva de alturas (0.25m, 0.50m, 0.80m, 1.10m, 1.40m, 1.60m y tope de 1.95m), gran escalinata monumental a +8.4m, carril de peralte asimétrico (+0.85m) y zanjas de zancada (1.5m y 2.4m).
    4. **Sector 3 (Sureste)**: Colina de pendientes graduadas con rampas medidas de 12°, 24°, 34°, 44° y 56°, además de carretera de montaña en curvas ascendiendo a la cima panorámica (+28m).
    5. **Sector 4 (Sur)**: Foso de vuelco con cuña de balanceo a 65° (>60° límite de vuelco) para ensayar la **Pataleta Hidráulica**, foso de contención y rampa suave de reincorporación al Hub.
    6. **Sector 5 (Oeste)**: Badlands y campo de rocas procedural con 75+ peñascos deterministas y canal "Rock Crawler" para testear la respuesta de suspensión y normal de superficie de los pies.
    7. **Sector 6 (Noroeste)**: El Gran Cañón de Ácido con fosa de 220m (`AcidHazard`), islas transitables y 3 puentes de pericia: Autopista blindada (16m), Puente fracturado (10m) y Pasarela Razor (7.8m, inclinación 4.2° sin barandilla).
    8. **Sector 7 (Noreste)**: Distrito urbano de ruinas y combate con rascacielos derruidos, autopista elevada de francotirador (+8m) y plaza central para la invasión de `EnemyBot`.
- [`Assets/Scripts/Editor/SetupWeaponsAndHUD.cs`](file:///Assets/Scripts/Editor/SetupWeaponsAndHUD.cs):
  - Menú `Mecha > Setup Weapons and Windshield HUD`: Reconstruye y enlaza automáticamente las armas (Gatling y Misiles), el HUD holográfico y los botones en la escena activa.
- [`Assets/Scripts/Editor/AssembleMechaPrototype.cs`](file:///Assets/Scripts/Editor/AssembleMechaPrototype.cs):
  - Menú `Mecha > Assemble Complete Prototype` / `PatoJuego > Assemble Full Prototype`: Genera desde cero toda la escena incorporando el Megaterreno de Pruebas de 600m x 600m, mecha, pato, botones, HUD y spawner.
- [`Assets/Scripts/AutoPlayTester.cs`](file:///Assets/Scripts/AutoPlayTester.cs):
  - Batería automatizada de pruebas en modo Play para validar avance, retroceso, giros, Gatling, Misiles, cooldowns, vuelco y reincorporación.

---

## 5. ⚠️ Directrices Cruciales para Futuras Modificaciones

1. **Compatibilidad hacia atrás**: Mantener siempre las propiedades públicas y aliases esperados por `AutoPlayTester.cs` (`LaserCooldown`, `IsGatlingFiring`, etc.) para no romper los tests automatizados.
2. **Espacio Local de Cabina**: Cualquier movimiento, fuerza o teletransporte del pato debe hacerse en **coordenadas locales de la cabina (`cockpitTransform`)** para evitar desincronización por las velocidades físicas del mecha.
3. **No Colocar Objetos Sólidos en el Campo de Visión Central**: El parabrisas debe mantener visión limpia. Toda la información del HUD debe ser nítida, de tipografía contenida (`charSize` entre 0.04f y 0.06f) y sin solapamientos.
4. **Armamento Actual**: Las únicas armas activas del mecha son la **Ametralladora Gatling** en el brazo izquierdo y el **Lanzamisiles** en el brazo derecho. No restaurar cilindros de rayos láser ni ondas de puño.
