# Escena Invernal de Rescate

Taller Práctico de Unity — *Módulo: Composición de Escenas y Control de Transformaciones*.

Una patrulla de policía queda atrapada y semienterrada por una tormenta de nieve: el motor
sigue encendido (humo en el escape), las sirenas giran y la ventisca arrecia sobre el entorno.

![Vista previa de la escena](docs/preview.png)

## Requisitos

- Unity **6000.6.6f1** (Unity 6) con el pipeline **URP**
- Linux / Windows / macOS

## Contenido de la escena

`Assets/Scenes/SampleScene.unity` contiene la jerarquía de 10 objetos exigida por el taller:

| # | Objeto | Componente principal | Función |
|---|--------|----------------------|---------|
| 1 | `Main Camera` | Camera | Encuadre lateral/diagonal del coche |
| 2 | `Moon_Light` | Directional Light | Luz general, azul pálido, intensidad baja |
| 3 | `Snow_Terrain` | Mesh generado | Suelo nevado con ventisqueros (morro más hundido) |
| 4 | `Police_Car` | Empty (Y = -0.5) | Objeto padre, semienterrado |
| 5 | `↳ Car_Mesh` | Mesh | Carrocería visible |
| 6 | `↳ Siren_Pivot` | Empty | Pivote de las sirenas (script) |
| 7 | `↳↳ Red_Light` | Point Light | Luz roja de emergencia |
| 8 | `↳↳ Blue_Light` | Point Light | Luz azul de emergencia |
| 9 | `↳ Exhaust_Smoke` | Particle System | Humo del escape |
| 10 | `Global_Blizzard` | Particle System | Nieve cayendo sobre la escena |

## Scripts

- `Assets/Scripts/SirenRotation.cs` — rota `Siren_Pivot` sobre su eje Y con velocidad
  ajustable desde el Inspector (`rotationSpeed`) usando `Transform.Rotate` y `Time.deltaTime`.

## Cómo abrir

1. Abrir el proyecto con Unity Hub (versión 6000.6.6f1).
2. Abrir `Assets/Scenes/SampleScene.unity`.
3. Pulsar **Play**.

## Estado

- [x] Jerarquía de 10 objetos
- [x] Iluminación (luna azulada + sirenas roja/azul rotando)
- [x] Sirenas alineadas con la barra del modelo: cada luz puntual y su bombilla
      emisiva (`Red_Bulb`, `Blue_Bulb`) en el centro de cada segmento de color
- [x] `SirenRotation.cs` funcionando en `Siren_Pivot`
- [x] Partículas: nieve (`Global_Blizzard`) y humo (`Exhaust_Smoke`)
- [x] Niebla y ambiente nocturno de ventisca
- [x] Modelo 3D del coche: patrulla low-poly de Kenney (CC0) con material propio
      (`Assets/Models/PoliceCar_Mat.mat`)
- [x] Nieve acumulada sobre el coche (`Car_Mesh/Snow_Cap`): malla generada con espesor
      **uniforme** sobre todas las superficies horizontales
- [x] Nevada ligera y uniforme en toda la escena (partículas lentas, ~100 × 100 m)
- [x] Terreno con relieve (`Assets/Models/SnowTerrainDrifts.asset`): pendiente bajo el
      coche (morro más hundido que la cola) y ventisqueros suaves alrededor, para que
      el enterramiento no sea uniforme

## Ajustar la nieve del coche

La capa es una malla generada (`Assets/Models/CarSnow.asset`), no un slider del Inspector.
Para cambiarla: **Tools → Nieve del coche** y mueve los valores:

| Parámetro | Qué hace |
|---|---|
| Espesor base | Nieve sobre todas las superficies horizontales del coche (uniforme) |
| Extra en el capó / frontal | Espesor añadido hacia el morro (0 = uniforme) |
| Solo caras con inclinación > | A partir de qué inclinación se acumula (más alto = solo lo plano) |
| Donde empieza el frontal | Desde qué parte del morro empieza el engrosado |

Pulsa **Regenerar nieve** para reconstruir la malla (también hay un botón para quitarla).

## Ajustar el humo del escape

**Tools → Humo del escape**. La ventana lee los valores actuales y los aplica en vivo:

| Parámetro | Qué hace |
|---|---|
| Posición | Punto de emisión local respecto a `Police_Car` (botones *Frente* / *Parte trasera*) |
| Partículas por segundo | Densidad del humo |
| Color inicial | Tinte base (el degradado va en Color over Lifetime) |
| Vida / Tamaño | Rangos mínimo y máximo |
| Velocidad / Gravedad | Con gravedad negativa el humo sube |
| Ángulo / Radio | Apertura del cono |

Incluye presets **ligero** y **denso**, y un botón para recargar los valores de la escena.

## Ajustar el enterramiento y el terreno

**Tools → Terreno y enterramiento**. Regenera la malla del suelo y mueve el coche:

| Parámetro | Qué hace |
|---|---|
| Nieve en el morro | Altura de nieve delante del coche (más alto = más hundido de frente) |
| Nieve en la cola | Altura detrás; si es igual al morro, el enterramiento es uniforme |
| Altura del coche | Mueve `Car_Mesh` en vertical (`Police_Car` se queda en Y = −0.5) |
| Ondulación | Cuánto varía el terreno (0 = plano perfecto) |
| Ventisqueros | Altura de los montículos alejados del coche |
| Zona limpia | Radio sin ruido alrededor del coche, para que el corte sea limpio |

Presets: **uniforme** y **hundido de morro**. También enlaza con *Nieve del coche*.

## Autor

Juan Pablo Ante — Taller de Laboratorio, Desarrollo de Videojuegos.

## Créditos

- Modelo del coche patrulla: [Kenney Car Kit](https://kenney.nl/assets/car-kit) — licencia **CC0 1.0**
  (dominio público). Ver `Assets/Models/Kenney-License.txt`.
