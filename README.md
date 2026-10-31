# 🦆 Pato-Mecha 3000

Prototipo de videojuego 3D cómico desarrollado en **Unity 6 (6000.3.8f1)** con **Universal Render Pipeline (URP)** y **UI Toolkit**.

Un pato caótico pilota un Mecha gigante desde una cabina física. Para controlar los movimientos y las armas del mecha, el pato no usa palancas ni pantallas táctiles, sino que debe **correr, saltar y dar culazos directamente sobre botones físicos gigantes instalados en el suelo de la cabina**.

---

## 🎮 Controles

### Cabina y Pato
- **`W`, `A`, `S`, `D` / Flechas:** Mover al pato por el suelo de la cabina (movimiento relativo a la cámara con inercia resbaladiza).
- **`Espacio`:** Saltar entre los botones físicos.
- **`E` / `Shift` / Clic Izquierdo (en el aire):** **¡CULAZO!** Pisotón en picado que activa los botones especiales de armas (**PUÑO** y **LÁSER**).
- **`Q` / Clic Derecho:** **¡CUAC!** Graznido cómico con texto flotante en pantalla.

### Cámara
- **Rueda del ratón (Scroll):** Zoom continuo y suave desde el interior de la cabina (plano corto) hasta contemplar el Mecha gigante completo en el exterior (hasta 42 m).
- **Arrastrar con clic (izquierdo o derecho):** Órbita libre en 360º alrededor del punto de mira.
- **`C`:** Alternar entre modos de cámara (Órbita Libre 360º, Dron Espectador con flechas, Fija de cabina).

### Pantalla de Game Over
- **`R` / `Espacio` / `Enter` / Clic:** Reiniciar la partida y volver a la cabina con 100% HP.

---

## 🔘 Botones Físicos de la Cabina

| Botón | Función | Mecánica |
| :--- | :--- | :--- |
| **▲ AVANZAR** | Desplaza el Mecha hacia adelante | Mantener pisado (*Hold*) |
| **▼ ATRÁS** | Desplaza el Mecha hacia atrás | Mantener pisado (*Hold*) |
| **◄ GIRO IZQ** | Rota el Mecha a la izquierda | Mantener pisado (*Hold*) |
| **GIRO DER ►** | Rota el Mecha a la derecha | Mantener pisado (*Hold*) |
| **💥 PUÑO** | Ataque cuerpo a cuerpo del brazo izquierdo | Activable con **¡CULAZO!** |
| **⚡ LÁSER** | Cañón láser continuo con alcance masivo | Activable con **¡CULAZO!** (Cooldown 2.0s) |

---

## 🛠️ Arquitectura Técnica

- **Motor:** Unity 6 (6000.3.8f1) URP.
- **Arte y Assets:** 100% procedural y primitivas 3D optimizadas (sin dependencias de assets externos).
- **Audio:** Sintetizador procedural de audio en tiempo real (`ProceduralAudio.cs`) mediante ondas matemáticas (senoidales, sierra, ruido blanco y barridos de frecuencia) para pasos, impactos, graznidos, láseres y explosiones.
- **UI:** Implementada íntegramente con **UI Toolkit** (`UIDocument`, `MechaHUD.uxml`, `MechaHUD.uss` y `MechaFontAsset.asset` SDF).
- **Física:** Simulación determinista en coordenadas locales de cabina para evitar desincronizaciones entre el Rigidbody del jugador y la plataforma móvil del Mecha.
- **Entrada:** Nuevo Input System de Unity (`com.unity.inputsystem`).
