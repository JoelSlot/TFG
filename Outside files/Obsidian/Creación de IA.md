Cada hormiga tendrá una inteligencia artifical simple, a través de la cual se simulará la colonia de hormigas. Sin embargo, el input humano debe ser significativo en el desarrollo de la colonia, para incentivar la interaccion del jugador. De esta forma ciertos aspectos del desarrollo deben ser reservados para el jugador (como por ejemplo el diseño de las cámaras y la elección del lugar de acopio de comida).

También deben poder haber otros nidos enemigos contra los que se tendrá que luchar. Se necesitará de una inteligencia artificial adicional que gestione un nido no jugador. 

De esta forma hay que desarroyar las siguientes IA:

- IA de hormias. Estas serán mayoritariamente similares entre sí.
	- IA de hormiga Reina
	- IA de hormiga Trabajadora
	- IA de hormiga Soldado
	- IA de hormiga Macho
	- IA de Larvas
- IA de otras criaturas: Orugas, gusanitos y larvas. Estos serán extremadamente simples.
- IA de administrador de nido. La IA que se ocupará de controlar nidos enemigos.

Hay que simular también la comunicación entre las hormigas. Esto se hace principalmente mediante feromonas, y define en gran parte como las hormigas buscan y recolectan comida. La IA de las hormigas tiene que implementar por tanto:

- Creación y seguimiento de caminos mediante feromonas
- Clasificación de caminos según sus recompensas

Duración estimada de 2 semanas.


## Planificación inicial de la estructura

