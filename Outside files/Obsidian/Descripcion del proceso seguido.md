
## Creación del terreno: Marching Cubes

El objetivo del TFG es crear un videojuego que simula una colonia de hormigas. De lo primero que se consideró fue crear un terreno dinámico que permitiría al jugador elegir un lugar cualquiera del mapa para el nido, ya que permitiría a las hormigas excavar en cualquier sección del terreno. La creación de mapas también se simplificaría, además de permitir crear un modo de edición de mapas para los jugadores.

### Funcionamiento del algoritmo de marching cubes

Cubos de marcha es el algoritmo que finalmente se implementó para representar la superficie del terreno. Es un algoritmo de gráficos por computadora originalmente publicado en SIGGRAPH en 1987 por Lorensen y Cline. Se usa para crear superficies poligonales como representación de una isosuperficie de un campo escalar 3D. En concreto, dado un campo tridimensional escalar de valores numéricos en el que se encuentra un volumen, cada punto dentro del volumen teniendo un valor mayor o igual que la constante isolevel y cada punto fuera del volumen teniendo un valor de menos de isolevel; el algoritmo puede crear la superficie aproximada entre el volumen y las afueras colocando una malla de triángulos entre ellos. Este proceso es simplificado dividiendo el espacio en cubos que comparten caras, donde cada lado del cubo será cortado por la superficie si de los dos vertices que lo forman 1 se encuentra dentro del volumen y el otro fuera. Así, dado los 8 vértices del cubo, hay 256 ($2^8$) posibles combinaciones de polígonos dentro del cubo que corten los lados. 
![[Pasted image 20241026140053.png]] IMG 1.1 Ejemplo de un cubo en el que la superficie corta los lados 2, 3 y 11

La dificultad a la que se enfrenta el algoritmo es la gran cantidad de combinaciones posibles y la necesidad de que aquellas combinaciones conecten correctamente entre los cubos para formar la malla. Soluciona esto y obtiene una gran velocidad de cómputo al usar lookup tables en los que se guardan todas las combinaciones correctas de triángulos. Los pasos que sigue el algoritmo para formar la malla son los siguientes:

1. Para cada cubo, se obtiene los ejes cortados por la isosuperficie usando los  valores de los vértices del cubo. Dado los vértices que se encuentran debajo del volumen, se obtiene un índice que se usa en la tabla de ejes para conseguir los ejes que la superficie del volumen corta. Por ejemplo, dado la imagen 1.1, el índice correspondiente sería 0000 0100 o 8. Se obtiene de la siguiente forma: 
```
   cubeindex = 0;
   if (grid.val[0] < isolevel) cubeindex |= 1;
   if (grid.val[1] < isolevel) cubeindex |= 2;
   if (grid.val[2] < isolevel) cubeindex |= 4;
   if (grid.val[3] < isolevel) cubeindex |= 8;
   if (grid.val[4] < isolevel) cubeindex |= 16;
   if (grid.val[5] < isolevel) cubeindex |= 32;
   if (grid.val[6] < isolevel) cubeindex |= 64;
   if (grid.val[7] < isolevel) cubeindex |= 128;
```


La tabla de ejes devuelve un número de 12 bits que representa cada eje del cubo. Los ejes cortados por la superficie tendrán valor 1 en la cadena, y los demás 0. Volviendo al ejemplo de la imagen 1.1, al buscar en la tabla de ejes usando el índice 8, se obtendría el número 1000 000 1100. Esto significa que los ejes cortados son el 2, el 3 y el 11 (teniendo un eje numero 0). 

2. Se calculan los puntos de intersección en los ejes cortados. Esto se hace mediante interpolación linear de  base de los valores de los dos vértices que forman el eje, y permite representar con más precisión el volumen. Dado los puntos P1 y P2 que forman el eje cortado y sus valores escalares V1 y V2 respectivamente, el punto de intersección viene dado por: $P = P_1 + (isolevel - V_1)(P_2-P_1)/(V_2 - V_1)$.

3. Por último, se crean las facetas formadas por las posiciones por las que la isosuperficie corta a los cubos. Se usa una segunda tabla, llamada tabla de triángulos, que contiene un array de números para cada posible combinación de facetas en un cubo. Cada array contiene para cada triángulo de esa combinación los ejes en los que se encuentran los puntos que forman dicho triángulo. Cada array contiene a lo sumo 5 triángulos, por lo que cada array tiene una longitud de 15 + 1 números (el último se usa para indicar cuando acaba el array). Los primeros 3 números del array indican el primero triangulo, los segundo 3 el segundo triángulo, etc. El algoritmo sigue creando facetas de triangulo dado un array hasta que se encuentra con un valor -1. El índice ya obtenido en el primer paso del algoritmo se usa también en esta tabla. 

Siguiendo el ejemplo anterior, el índice 8 en la tabla de triángulos apunta a el array {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}. 

El pseudocódigo del algoritmo sería:
```
mallaTriangulos <- empty
Para cada cubo:
	índice <- obtenerIndice(cubo.vértices)
	ejes <- tablaEjes[índice]
	Para cada eje en ejes:
		P1 <- eje.P1; V1 <- cubo.valor(P1)
		P2 <- eje.P2; V2 <- cubo.valor(P2)
		eje.puntoCorte <- P1 + (isolevel - V1)(P2 - P1)/(V2 - V1)
	T <- tablaTriángulos[índice]
	Para i en [0..4]
		si T[i*3] == -1
			BREAK
		P1 <- eje[T[i*3]]
		P2 <- eje[T[i*3 + 1]]
		P3 <- eje[T[i*3 + 2]]
		mallaTriangulo.añadirTriangulo({P1, P2, P3})
```

### Implementación del marching cubes

El terreno como malla de triángulos se representa en unity como un objeto Mesh. Los datos del objeto mesh que se crea en unity se adjunta a un objeto mediante el componente MeshFilter. Si se quiere mostrar el mesh, el objeto también necesita un componente MeshRenderer. Por ultimo, ya que se quiere tener a las hormigas caminando  por el mesh, se necesita poder collisionar con él, por lo que el objeto también usa un componente MeshCollider.

Originalmente se implementó el marching cubes como tal para generar todo el terreno del mapa. Sin embargo, al observar su funcionamiento quedó claro que calcular y generar la malla tridimensional entera cada vez que se editara 1 parte del mapa era demasiado carga de cómputo. Además, visualizar constantemente el mapa entero podría suponer una exigencia demasiado grande al GPU. Por eso se optó a dividir el terreno en partes equitativas pequeñas y renderizarlos juntos en el juego. Estas partes se denominan chunks, es decir, pedazos de terreno. Gracias a estos chunks, al editar partes del mapa solo hace falta recargar las piezas de la malla afectadas por este cambio.

Cada chunk contiene sus propios datos relevantes al pedazo de terreno que representan, como sus dimensiones y su posición. También tiene asociado los componentes necesarios para gestionar su porción de la malla: El MeshFilter para crear y gestionarlo, el MeshRenderer para poder visualizarlo, y el MeshCollider para que pueda tener colisiones. Contiene un puntero al objeto WorldGen que lo creó para poder obtener datos relevantes al mapa del terreno. Lo más importante que obtiene del WorldGen es el acceso su objeto terrainMap: representa todos los valores de la cuadrícula tridimensional que se usan en el algoritmo de MarchingCubes para crear la malla. La lista de vértices y triángulos que forman la malla son generados mediante la función MarchCube.
 ![[Pasted image 20241102163303.png]]Imagen: Las clases chunk y WorldGen con los atributos y funciones relevantes a la creación y gestión del mapa

Además del campo escalar que forma el mapa, los otros datos como la cantidad de chunks que se usan para formar el mapa y las dimensiones X, Y y Z de los chunks también se encuentran en la clase WorldGen, encargado de la creación de los objetos Chunk. Guarda los chunks en un diccionario, asociandolos a su posición relativa dentro del mapa. Sus funciones son las siguientes:

- GenerateChunk()
	Genera todos los objetos chunk necesarios para

En el script, se implementó como en la imagen im 1.2. La función GetCubeConfig recibe un array de los valores de los vértices en orden y devuelve el índice correspondiente.
  
NOT FINISHED GET UR ASS BACK ON THIS
## Obtención de assets

Los modelos 3d se obtuvieron online, ya que crearlos desde cero seria una carga de trabajo digno para un TFG aparte.  Algunos modelos si fueron editados en blender para crear distintas versiones, como por ejemplo la hormiga reina a partir de la hormiga base.
### Modelo de la hormiga

El modelo de hormiga trabajadora por defecto lo he podido obtener en [Sketchfab](https://sketchfab.com/3d-models/lowpoly-ant-cb84febbdce54d0d9c3cfcf2401ccccc). Es un modelo low poly, lo cual funciona bien con la apariencia low poly innata del marching cubes que se usará para el terreno. Sin embargo, no está riggeado. Tendremos que crear huesos para el modelo y ajustar las distintas partes de la hormiga para mover con ellas.

![[Pasted image 20240916165614.png]]

## Riggeando la hormiga

Para riggear la hormiga y eventualmente crear animaciones, se usó blender. Hubo que crear un esqueleto de tal forma que cada parte de la hormiga que tuviera que moverse se correspondiera a un hueso. La parte principal del cuerpo de la hormiga está formada por el tórax, la cabeza y el abdomen. La cabeza y el abdomen son rígidos, por lo que se correspondieron con un hueso cada uno. El abdomen usó 2 huesos, ya que es una parte flexible. Luego las patas un hueso por cada segmento, un hueso para cada mandíbula y 2 huesos para cada antena. 

![[Pasted image 20240916174524.png]]

![[Pasted image 20240916174614.png]]

![[Pasted image 20240916174715.png]]

El proceso de ajustar las distintas partes del modelo a los huesos se hizo mediante "weighted painting". Consiste en usar un pincel para destacar aquellas superficies del modelo que quieres que se muevan. Se seleccionan hueso a hueso, y para cada uno ilustras las partes a las que corresponden. Como la mayoría de las partes de la hormiga son exoesqueleto no elástico, tan solo hizo falta seleccionar los vértices relevantes al hueso, poner su valor a 1, hacer selección inversa y poner el resto a 0. Las únicas excepciones a esta regla fueron los dos huesos de abdomen y los cuatro huesos de las antenas, en los cuales se usó un degradado. Esto se hizo para simular su elasticidad.

![[Pasted image 20240916185141.png]]
![[Pasted image 20240916184531.png]]

El esqueleto es simétrico, lo cual simplificó varios aspectos de la animación, ya que se podía animar solo una mitad del cuerpo y tenerlo reflejado en la otra mitad. Para habilitar eso, tuvieron que ser nombrados de forma específica los pares de huesos: los de la parte izquierda y derecha tienen el mismo nombre a diferencia de una L en los de la izquierda y una R en los de la derecha.

## Creación de animaciones

Blender proporciona un editador de animaciones útil para poner en movimiento inmediatamente los modelos con los que trabajo. Con la ayuda de una [curso online gratuito](https://youtube.com/playlist?list=PLcaQc6eQjXCwhUof1Fdq1xi6dw4m0taHX&si=CBaNAZv9Gnxqmiug) aprendi a usarlo lo suficiente para crear algunas animaciones simples. La primera de esta, y la más importante fue el ciclo de caminar de la hormiga. 

### Walk cycle

Inicialmente pensaba implementar animación procedural para el movimiento de las patas de la hormiga. Esto haría más realista su forma de moverse por el terreno, pero por motivos de tiempo parece ser poco realista. He creado una animación de caminar simple inicial, y si en el futuro del proyecto hay tiempo implementaré la idea original.

Durante el riggeo usamos Inverse Kinematics y creamos 6 Constraints, uno para cada pata. Esto fue para simplificar el proceso de la animación: cada pata se mueve con el punto de agarre (un cuerpo vacío) correspondiente y tan solo hay que mover esos para simular el movimiento. 
![[Pasted image 20240917200647.png]] En naranja: los 6 IK que dirigen los extremos de las patas.

Blender tiene 3 formas de animar mediante keyframes: Timeline, graph editor y dope sheet. Todos tienen sus ventaja e inconvenientes, pero para la animación de caminar vino mejor el graph editor. Esto se debe a que simplemente hay que mover los objetos vacíos que dirijen las patas de una forma repetitiva. Las hormigas mueven sus patas de 3 en 3, lo que simplifica el proceso de animar la forma de caminar.
![[Animacion de una pata de la hormiga.png]] El grafo de movimiento de una de las patas. Solo se mueven en el plano Y y Z.

![[Pasted image 20240917201830.png]] Todos los movimientos superpuestos. Destacan 2 grupos de movimiento.

Las patas de la hormiga se mueven bien, pero el tórax es demasiado influenciado por estos movimientos. Para evitar esto, editamos el "stiffness" del tórax para limitar el efecto que tienen los demás huesos sobre él. También limitamos los IK a influir solo a 3 huesos de distancia, para que no sean afectadas partes del rig fuera de las patas.
![[Thorax IK stiffness.png]]
Ahorró mucho trabajo el uso de estos agarres, pero aún así faltaba retocar cosas del modelo. Hubo que editar rotaciones heredadas y movimientos de otros huesos para evadir angulos extraños y sobrenaturales. Las ultimas partes de las patas por ejemplo siempre mantendrían el mismo ángulo global, y el tórax se movía demasiado con las patas.
![[Pasted image 20240917202327.png]] Ejemplo de un angulo extraño al no girar la pata.
![[Pasted image 20240917202450.png]] Ángulo arreglado


La animación funciona bien, pero no se mueve su cabeza. Las hormigas buscan con sus antenas los caminos de feromonas que ellos y otras hormigas marcan mientras caminan. Quise añadir esto a la animación, pero no queria editar la animación anterior. Para ello usé el menu de Nonlinear Animation. Permite combinar acciones distintas en animaciones individuales. Creé una acción de búsqueda con las antenas y luego la combiné con el de caminar para crear una única animación en la que camina y busca.

![[Sobreponer antena y caminar.png]]

### Idle animation

Para que las hormigas no se queden inmóviles mientras no se muevan por el mapa, hace falta tener una animación "idle". Considerando que las hormigas siempre están sintiendo el terreno mediante sus antenas, se creó una animación de 70 frames en las que la hormiga mueve la cabeza de derecha a izquierda mientras siente el terreno con sus antenas. En unity, esta animación se mostrará cada de vez en cuando mientras la hormiga está inmóvil.

### Turn around

Solo hizo falta la animación de girar mientras la hormiga está inmóvil, ya que para cuando camina hacia delante y se gira a la vez  la animación estándar de caminar hacia delante es suficiente. La animación consistió en la hormiga girando 24 grados en 24 frames para ayudar a simplificar el proceso de animación. En unity, el objeto hormiga gira dentro del motor 3D, por lo que en la animación misma el cuerpo principal de la hormiga siempre mirará hacia delante, mientras que las patas se mueven como si se girar la hormiga. Para conseguir esto de forma simple, se siguieron los siguientes pasos: Primero se registró la pose default de la hormiga como el primer frame de animación. Luego se giraron todos los IK constraints de las patas 24 grados alrededor del centro de la hormiga y se registraron en el último frame para que las patas se fueran girando con velocidad constante. Finalmente se creó para cada pata un paso de 24 grados en la dirección contraria a la que estaban girando, de tres en tres. Tres al principio, despues de lo cual giran a su posición inicial; Tres al final, llegando a esa posición al dar un paso después de girar. También incliné el tórax de la hormiga un poco para que mirara hacia donde girara. Para hacer la animación de girar en la otra dirección, simplemente se copió y pegó la original en modo espejo.

### Attack

La animación del ataque hace uso de dos principios de animación: Buildup y overshoot. La hormiga se prepara para dar un mordisco moviendo primero su cuerpo hacia atrás, antes de disparar hacia delante (buildup). Luego en la cúspide de la distancia que hace la hormiga, al final de su trayecto, tiene dos frames en los que llega más lejos de lo que deberia parecer posible, antes de volver a la distancia esperada. Estos dos frames de overshoot son difíciles de ver, pero hacen que se sienta más la velocidad del ataque. La hormiga abre las mandíbulas al principio del movimiento hacia delante, y las cierra rápidamente para dar más impacto.

### Picking up


La animación consiste en la hormiga agacharse para recoger un objeto después de arquearse hacia atrás para observarlo con sus antenas. La animación consiste de 34 frames. El objeto mismo no forma parte de la animación, sino que en el juego se colocará un objeto en esa posición que la hormiga podrá levantar. La primera mitad de la animación, el tocar el objeto con las antenas, le da una sensación tanto de curiosidad como precaución a la hormiga. La animación acaba con la hormiga en la pose inicial por defecto, excepto que su cabeza está inclinada hacia arriba, sus antenas no apuntan directamente hacia delante y sus mandíbulas se encuentran abiertas. Esto se hace para simular que ahora la hormiga lleva en sus mandíbulas un objeto.

### Carrying

La animación de llevar un objeto encima mientras la hormiga permanece inmóvil se creó a partir de la pose por defecto de la hormiga. La cabeza se levantó con las mandíbulas abiertas mientras que las antenas, dejando espacio para el objeto que la hormiga llevará en las mandíbulas, suben y bajan con un ritmo tranquilo. Estos cambios se combinan con otras animaciones usando NLA (Non Linear Animation) strips para crear varias versiones de la hormiga llevando un objeto mientras se mueve. Las creadas son las siguientes: Caminar, girar hacia la izquierda y girar hacia la derecha.
### Putting down

La animación de dejar en el suelo un objeto simplemente consiste en la hormiga agachándose hasta llegar con la cabeza al suelo y abriendo las mandíbulas para soltar el objeto. Luego vuelve a retomar su pose por defecto cerrando sus mandíbulas, poniendo rectos sus antenas y incorporándose.

### Getting hit

Para la animación de ser golpeado es importante crear un impacto. Para ello, la hormiga retrocede hacia atrás, mueve su cabeza hacia arriba, contorsiona sus antenas y desdobla su abdomen hasta conseguir una pose anormal. Esto ocurre de forma muy rápida, en 3 frames. La hormiga se mantiene en la pose durante 6 frames, para simular lo que en videojuegos se denomina hitstun: un periodo que ocurre tras ser golpeado en el que el luchador muestra una postura dolorosa durante unos frames para aumentar el impacto del golpe. Despues de este periodo, la hormiga lentamente se incorpora y retoma su pose por defecto.

### Dying

Para la animación de muerte se crearon dos versiones: una "estática" y otra "dinámica". La estática muestra la hormiga, después de haber sido herido, doblar las patas y derrumbarse al suelo hasta quedarse boca arriba. Esta animación cuenta con el hecho de que se encuentra en el suelo, y por tanto la gravedad atrae la hormiga hacia abajo. Sin embargo, considerando que las hormigas en el juego podían escalar paredes y techos, se creó una segunda versión; la dinámica. La animación dinámica no mueve el tórax de la hormiga, sino que cuenta con el juego quitando el rigidbody de la hormiga en cuanto se muera para que el cuerpo pueda caer hacia abajo. Consiste en la hormiga contrayendo las patas y acabando en una pose compacta e inmóvil. Esta pose final es útil ya que las hormigas muertas pueden ser llevadas por otras hormigas enemigas a sus nidos para ser consumido.

### Dead

Cuando la hormiga se encuentra muerta, en vez de no mover se creó una animación en la que las patas de la hormiga muestra contracciones repentinas. Esto simula el fenómeno real que ocurre en los insectos, y da más personalidad al juego. Al igual que con la animación de morir, existen dos versiones. La inicial estática en la que la hormiga se encuentra bocabajo con las patas dando espasmos hacia arriba, y la segunda dinámica en la que el tórax del modelo no se ha movido de su posición inicial.

## Fisicas y movimiento de las hormigas


### Movimiento

Antes de cargar el modelo de la hormiga en el juego se creó un sistema de movimiento usando un cubo simple con un rigidbody aplicado para las colisiones.
![[Pasted image 20240911133252.png]]
![[Pasted image 20240911133037.png]]
La esfera azul clara indica el centro de masa del rigidbody. De esta forma, si la "hormiga" se encuentra bocaabajo rotará hasta alinearse con el suelo. Esto es importante para evitar que agentes de hormigas puedan quedarse pillado en el mapa.

Para gestionar el movimiento de la hormiga en el entorno, se le asignaron dos estados generales posibles: el de estar en una superficie (grounded) y el de estar cayendo (falling). El vector de fuerza de gravedad solo se aplicaría a la hormiga al no estar con los pies en alguna superficie (en el estado falling).

Para decidir si se encuentra en el estado grounded o falling, el agente tiene un raycast corto desde la parte de abajo de la hormiga hacia fuera. Si toca una superficie, se considera que se encuentra en el suelo y el vector de gravedad será desactivada hasta dejar de detectar la superficie.

Despues de programar que la hormiga no se cayera al estar en estado grounded, hubo que implementar su movimiento. Debió moverse acorde a la superficie para no separarse y caerse. Por lo tanto, en vez de que la hormiga se moviera hacia donde mirara, se decidió hacer que se desplazara acorde al vector de su dirección proyectada sobre la superficie del suelo. Para ello se usó la función Vector3.ProjectOnPlane.

 Vector de movimiento resultante de proyectar la dirección sobre la superficie en la que se encuentra (amarillo)

Se escribió un simple código para mover la hormiga en la dirección del vector proyectado al pulsar la tecla de flecha hacia arriba. Proporcionó movimiento aceptable en terreno plano, pero mostraba problemas al subir por elevaciones. Debido a su gran rigidbody, al subir por elevaciones el raycast se alegaría demasiado de la superficie del terreno, poniendolo en modo falling y dejando inamovible el agente. Para solucionar esto se añadieron más raycasts a los extremos de la hormiga, con el objetivo de siempre poder sentir parte de la superficie sobre la que se encontraría el agente.
 imagen a): ejemplo de una situación en la que el agente no puede "ver" el terreno debido a una elevación
![[Raycast Collage.png]]
Al tener más raycasts, se necesitaba gestionar cuantas harían falta que sintieran el terreno para que el agente entrara en el estado grounded y se pudiera mover. Se decidió inicialmente 3 de los 5. La proyección del vector de movimiento se hacía sobre el plano resultante de la media de los planos del terreno que los raycasts vieran. Si dos raycasts chocan con el mismo plano, ese plano cuenta como dos, era la fórmula. Esto permitió al agente subir cuestas con más facilidad, pero no evitó que se quedara pillado del todo (im b). Se probó disminuir los raycasts activados necesarios a solo 2 para el modo grounded, lo cual dificultaba aún más que se quedara pillado pero no lo evitaba del todo. (im c: solo un raycast ve el terreno).

Otro obstáculo fue tener que encontrar una forma de hacer que la dirección en la que miraba el agente fuera perpendicular al terreno. El agente podría acabar mirando en una dirección diagonal en respecto al terreno, lo que podia causar que se separase de ella al moverse por superficies curvadas. Esto no mostraba ser un problema demasiado grande en el caso de moverse por superficies cóncavas (F 1), pero destacaba más en superficies convexas (F 2). Se intentó arreglar esto cambiando la dirección del agente según el terreno sobre el que se encontraba. La dirección hacia arriba del agente se ajustó a la de la media de los normales de los terrenos que detectaran los raycasts. Esto permitió al agente mover a velocidades más rápidas sin desconectarse del terreno, pero causaba problemas al subir cuestas y caminar por terrenos irregulares: su cambio de dirección repentino causaba que los extremos del agente podrían chocar con el terreno y separar a la hormiga de ella.

Otra dificultad con el método de ajustar según las normales del terreno fue que los raycasts podian detectar terrenos con orientaciones muy distintas, lo que causaba cambios bruscos en su dirección. Su nueva orientación a su vez detectaría otros terrenos, por lo que cambiaría de nuevo bruscamente de dirección en el siguiente frame. A veces el agente se quedaría pillado cambiandose continuamente de una dirección a otra. Para remediar este problema, se intentó acercar los raycasts. Si su distancia no fuera mayor a la longitud media de los ejes de los triángulos del terreno no debería poder detectar más de 2 distintas en cada dirección. Esto evitó que se quedara pilaldo cambiando de dirección, pero volvió a causar que el agente se quedara pillado en superficies cóncavas.

![[RaycastExample 1.png]] Imagen F: trayecto del agente sobre superficies inclinadas sin un corrector de dirección.

Se decidió en un compromiso final: reducir el tamaño del rigidbody del agente (im e). Con esto se sacrificó cierto realismo, ya que ahora los los extremos de los modelos de las hormigas podrían atravesar el terreno más fácilmente. Sin embargo, en cuanto a funcionalidad solucionó tanto el problema del agente quedándose pillado en superficies convexas como el problema del agente quedándose pillado en superficies irregulares al ajustar su dirección.

Hubo un problema principal más que solucionar: el de mantener el agente sobre las superficies convexas al moverse por ellas a mayores velocidades. Como podemos ver en la imagen F, en el primer caso de subir una cuesta, la cuesta misma ayuda a corregir la dirección del agente a cualquier velocidad. En el segundo caso, el objeto eventualmente se separa del terreno si el agente se mueve lo suficientemente rápido, pero debido a la gravedad en estado falling cae hasta detectar de nuevo el terreno. En el último caso, donde un agente intenta subir una cuesta curvada desde debajo del terreno, al dejar de detectar el terreno cae hacia abajo. El ajuste de su dirección mejoró el problema, pero no evitaba que el agente se alejara del terreno.

Hubo que encontrar una forma de hacer que el agente se acercara al terreno sobre el que se situaba. Se intentó hacer esto de tres modos. La inicial fue ir ajustando la posición del agente manualmente para moverlo hacia el terreno. Esto provocaba problemas con 

NOT FINISHED PLS DO THIS SOON!!!

- Attempt to solve the issue of not following terrain direction
	- make up direction of agent the medium of the normals under it
	- Problem: abrupt changes in direction jerks the agent around and doesn't allow it to scale walls and uneven terrain properly.
- smaller rigidbody allows better navigation and rarely gets stuck
	- Smaller than edges
- Problem: ant detaches from rounded surfaces
	- Solution: make it move towards the surface
	- causes it to twitch and clip
	- Solution 2: add gravity
	- problem: too slow, detaches ant if its too fast on rounded surface
	- Solution 3: use addforce instead, solves most problems



### Pathfinding

Las hormigas deben poder volver al nido y salir de ella en busca de comida. En la naturaleza esto lo hacen mediante pheromonas. La gran mayoria de hormigas al explorar dejan un camino de pheromonas por donde pasan, que pueden usar para volver. Hormigas exploradores crearán un camino de pheromonas especiales hacia el nido al encontrar comida, que otros trabajadores usarán ([source](https://resjournals.onlinelibrary.wiley.com/doi/10.1111/j.1365-3032.2008.00658.x#:~:text=The%20process%20of%20creating%20a,that%20are%20not%20always%20understood.)). Inicialmente quisa usar pathfinding para simular este comportamiento pero ya que se encuentra todo en un espacio 3D dinámico, es dificil implementar un pathfinding tradicional. Tras informarme sobre muchos métodos de buscacaminos en 3D (INSERTAR SOURCES POSIBLES) me di cuenta de que sería mucho más simple y realista imitar el sistema de pheromonas que usan las hormigas. Usando simples vectores, las hormigas se acercarán a distintas fuentes de olor y serán capaz es de seguir caminos largos y complejos. 


### Implementación de las feromonas

Inicialmente se hicieron tests para ver la viabilidad de el sistema de feromonas. Se necesitaba una forma de asegurar que una hormiga pudiera moverse hacia las feromonas que detectara y a que distancia esto era viable. Como representar las feromonas en el entorno también fue una cuestión que debió ser contestado. El primer paso fue 



## Modelo de larva


La larva es un elemento del mapa comestible que vivirá en grupos de múltiples larvas. El modelo original que se obtuvo en sketchfab 

https://sketchfab.com/3d-models/larva-14877198919544f5b43cef3e86e7b89f

![[Pasted image 20241025132043.png]]
![[Pasted image 20241025132257.png]]

![[Pasted image 20241025132335.png]]

