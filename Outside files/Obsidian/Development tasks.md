
## Creación y obtención de assets
Assets necesarios:
- Modelos 3D de las hormigas:
	- Hormiga trabajadora
	- Hormiga reina
	- Huevo
- Modelos de terreno
	- Rocas
	- Palos
	- Plantas
	- Restos de tierra
- Modelos de comida
	- Insectos simples
		- larvas
	- Semillas y granos

**Tiempo previsto: 1 semanas**

## Creación de animaciones
Animaciones necesarias
- Animaciones de hormigas
	- Caminar
	- Excavar
	- Recoger/depositar
	- Poner huevos (reina)
	- Crecer
		- Huevo-hormiga
	- Comer
		- Trabajadora comiendo
		- Reina comiendo
	- Luchar
	- Muerte
- Animaciones de otros entes
	- Movimiento de plantas
	- movimiento larvas

**Tiempo previsto de completación: 1.5 semana**

## Implementación de las accions
Aquí debo de implementar todas las acciones que pueden tomar las hormigas en el código, haciendo uso de las animaciones creadas.

**Tiempo estimado 3 dias**

## Controles y cámara de jugador
Se tienen que implementar controles de la cámara del jugador de tal forma que sea facil navegar el nido y las afueras. Para simplificarlo, se dividirá en **modo nido** y **modo afueras**. El modo nido debe mostrar bien las cámaras y túneles, haciendo transparente sus paredes más cercanas a la cámara. Adicionalmente, se incluirá un modo de cámara que seguirá una hormiga seleccionada.

Tiempo esperado de 2 días.
## Implementación de ciclo de vida de hormigas
Se debe de programar el ciclo de crecimiento de la hormiga, desde un huevo hasta su forma adulta. Hay que tener en cuenta la edad a la que mueren y que pueden morir de hambre.

**Tiempo estimado: 2 dias**

## Control del nido
### Sistema de excavación

Se trata de permitir que las hormigas excaven túneles y cámaras en el terreno, y que el jugador pueda mandar a la colonia a excavarlos. Se necesitará de dos elementos:

- Funciones que permitan al jugador seleccionar que quieren que se excave y donde.
- Funciones que permita a la hormiga remover tierra poco a poco de un área que debe ser excavado.

Las cámaras son planas y redondas, y el jugador podrá escoger algunas versiones distintas y modificar su tamaño. Los túneles son mayoritariamente verticales, y conectan estas cámaras.

Tiempo estimado: 3 dias

### Sistema de asignación de funciones a cámaras
Cada cámara del nido tiene una función especifica. El jugador podrá elegir cual tiene cada y reorganizarlos como quiera. Los tipos de cámara son:

- Almacenamiento de comida
- Cámara de la reina. Aqui pondrá todos sus huevos y los trabajadores le traen comida.
- Cámara de huevos. Los huevos se mueven hasta aquí para ser vigilados. En cuanto nacen, son son movidos hacia una cámara guardería.
- Cámara guardería para larvas y capullos

Tiempo estimado: medio dia

## Control de las hormigas
El jugador tomará decisiones generales y directas. Estas serán las siguientes:
- Decisiones generales
	- Designar áreas a explorar
	- Designar lugares donde excavar ([[Control del nido]])
	- Designar áreas de patrulla, defensa, etc...
	- Designar áreas a atacar
- Decisiones directas: el jugador controla directamente un grupo de hormigas
	- Mandar a un lugar
	- Mandar a recolectar/mover algo
	- Mandar a atacar a otras hormigas
	- Mandar a patrullar un área
	- Mandar a comer
	- Mandar a limpiar algo

**Tiempo estimado: 3-4 dias**

## Creación de IA
Cada hormiga tendrá una inteligencia artifical simple, a través de la cual se simulará la colonia de hormigas. Sin embargo, el input humano debe ser significativo en el desarrollo de la colonia, para incentivar la interaccion del jugador. De esta forma ciertos aspectos del desarrollo deben ser reservados para el jugador (como por ejemplo el diseño de las cámaras y la elección del lugar de acopio de comida).

También deben poder haber otros nidos enemigos contra los que se tendrá que luchar. Se necesitará de una inteligencia artificial adicional que gestione un nido no jugador. 

De esta forma hay que desarroyar las siguientes IA:

- IA de hormias. Estas serán mayoritariamente similares entre sí.
	- IA de hormiga Reina
	- IA de hormiga Trabajadora
- IA de otras criaturas: larvas. Estos serán extremadamente simples.
- IA de administrador de nido. La IA que se ocupará de controlar nidos enemigos.

Hay que simular también la comunicación entre las hormigas. Esto se hace principalmente mediante feromonas, y define en gran parte como las hormigas buscan y recolectan comida. La IA de las hormigas tiene que implementar por tanto:

- Creación y seguimiento de caminos mediante feromonas
- Clasificación de caminos según sus recompensas

**Duración estimada de 2 semanas.**

## UI del juego
Se debe de crear una UI para el jugador accesible que transmita toda la información necesaria.

**Tiempo esperado de 3 dias.**

## Implementación de mapas, missiones y retos
Con el juego base terminado, hay que diseñar una variedad de mapas en los que jugar, además de distintos modos de juego (sandbox, conquista, etc) y retos para motivar al jugador.

**Tiempo esperado: 2 días**


![[Pasted image 20240716160956.png]]