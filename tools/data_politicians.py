"""Source table for data/politicians.json (run to regenerate the JSON)."""
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STATS = ["carisma", "oratoria", "aguante", "dialectica", "templanza", "agilidad"]

P = [
 (1, "becario", "Becario de Gabinete", ["mediatico"], "madrid", [45, 40, 40, 50, 45, 55], 200, 55, (2, 14), "mediatico",
  [("rueda_de_prensa", 1), ("foto_de_familia", 3), ("titular_viral", 8), ("filtracion", 12)],
  "Lleva la carpeta abrazada como si fuera un salvavidas. Hace fotocopias de las fotocopias por si acaso."),
 (2, "asesor", "Asesor Áulico", ["mediatico"], "madrid", [60, 55, 55, 70, 60, 75], 90, 120, (3, 30), "mediatico",
  [("rueda_de_prensa", 1), ("titular_viral", 1), ("filtracion", 14), ("hilo_polemico", 20)],
  "Habla por dos móviles a la vez y nunca cuelga ninguno. Su sonrisa ya ha sido testada en tres encuestas."),
 (3, "jefe_gabinete", "Jefe de Gabinete", ["mediatico"], "madrid", [80, 70, 75, 100, 85, 95], 45, 200, None, "mediatico",
  [("titular_viral", 1), ("filtracion", 1), ("hilo_polemico", 1), ("powerpoint_letal", 34)],
  "Controla la agenda, el relato y el mando del aire acondicionado. Sus gafas oscuras no se quita ni en el cine."),
 (4, "tertulianete", "Tertulianete", ["mediatico", "tuitero"], "madrid", [50, 35, 35, 55, 40, 60], 190, 60, (5, 22), "mediatico",
  [("rueda_de_prensa", 1), ("hilo_polemico", 5), ("titular_viral", 10), ("zasca", 16)],
  "Opina de todo con un micro de juguete. Aún no ha terminado su primera frase, empezada hace dos temporadas."),
 (5, "todologo", "El Todólogo", ["mediatico", "tuitero"], "madrid", [85, 55, 65, 95, 75, 85], 60, 175, None, "mediatico",
  [("titular_viral", 1), ("zasca", 1), ("hilo_polemico", 1), ("ratio", 26)],
  "Experto en volcanes por la mañana y en fútbol por la tarde. Trae su propio atril por si le faltan tribunas."),
 (6, "infografia", "Doña Infografía", ["tecnocrata"], "madrid", [60, 40, 55, 85, 75, 60], 120, 130, None, "tecnocrata",
  [("recorte_presupuestario", 1), ("informe_300", 1), ("foto_de_familia", 8), ("powerpoint_letal", 18)],
  "Convierte cualquier conversación en un gráfico de barras. Su puntero láser no tiene botón de apagado."),
 (7, "puertagiratoria", "El Puertagiratoria", ["lobista"], "madrid", [70, 75, 70, 60, 65, 55], 90, 140, None, "lobista",
  [("influencia", 1), ("puerta_giratoria", 1), ("inauguracion", 10), ("maletin", 18)],
  "Nadie sabe si entra o si sale. Su reloj cuesta más que un ministerio pequeño."),
 (8, "trending", "Trending Topic", ["tuitero"], "madrid", [45, 50, 35, 60, 40, 80], 170, 80, None, "tuitero",
  [("hilo_polemico", 1), ("zasca", 1), ("bloqueo", 8), ("ratio", 20)],
  "Vive pegado a un móvil más grande que él. Dura en lo más alto lo mismo que un yogur fuera de la nevera."),
 (9, "ventanilla", "Ventanilla Cerrada", ["burocrata"], "madrid", [70, 45, 85, 40, 70, 25], 150, 100, None, "burocrata",
  [("sello_oficial", 1), ("vuelva_usted_manana", 1), ("silencio_administrativo", 8), ("expediente_infinito", 16)],
  "Su horario de atención es de 9:00 a 9:05 los martes alternos. Su café, eso sí, siempre está caliente."),
 (10, "pregonero", "Pregonero", ["baron"], "andalucia", [50, 55, 45, 35, 40, 45], 200, 60, (11, 16), "baron",
  [("dedazo", 1), ("arranque_campana", 3), ("promesa_imposible", 8), ("inauguracion", 12)],
  "Anuncia la feria, las obras y el tiempo con la misma bocina y la misma alegría contagiosa."),
 (11, "alcalde_feria", "Alcalde de Feria", ["baron"], "andalucia", [70, 75, 65, 50, 55, 60], 90, 125, (12, 30), "baron",
  [("dedazo", 1), ("inauguracion", 1), ("clientelismo", 16), ("vara_de_mando", 24)],
  "No hay caseta en la que no le inviten. Lleva un clavel en la solapa que cambia cada hora."),
 (12, "baron_sur", "Barón del Sur", ["baron"], "andalucia", [100, 100, 90, 65, 80, 65], 45, 205, None, "baron",
  [("dedazo", 1), ("clientelismo", 1), ("inauguracion", 1), ("vara_de_mando", 1)],
  "Patriarca de la comarca. Su capa granate tiene más años que la mitad del pueblo."),
 (13, "pico_oro", "El Pico de Oro", ["populista"], "andalucia", [65, 55, 50, 80, 55, 70], 110, 130, None, "populista",
  [("promesa_imposible", 1), ("arranque_campana", 1), ("bano_de_masas", 14), ("discurso_encendido", 22)],
  "Cuando abre la boca saltan chispas. Promete un aeropuerto en cada pueblo, incluso en los que no tienen pueblo."),
 (14, "mitinera", "La Mitinera", ["populista", "mediatico"], "andalucia", [70, 60, 60, 85, 65, 75], 75, 155, None, "mitinera",
  [("promesa_imposible", 1), ("titular_viral", 1), ("bano_de_masas", 1), ("discurso_encendido", 20)],
  "Lleva su propio escenario plegable y un cañón de confeti. Su megáfono se oye desde la provincia de al lado."),
 (15, "jornalero", "Jornalero Indignado", ["activista"], "andalucia", [55, 60, 50, 30, 45, 45], 180, 65, (16, 22), "activista",
  [("pancarta", 1), ("sentada", 4), ("recogida_de_firmas", 10), ("huelga_general", 18)],
  "Trabaja de sol a sol y aún le queda energía para fruncir el ceño. Su sombrero ha visto mil asambleas."),
 (16, "lider_marea", "Líder de la Marea", ["activista"], "andalucia", [85, 90, 75, 55, 70, 70], 70, 165, None, "activista",
  [("pancarta", 1), ("recogida_de_firmas", 1), ("sentada", 1), ("huelga_general", 1)],
  "Nunca camina solo: una pequeña multitud le sigue a todas partes. Su pancarta está en blanco para no equivocarse."),
 (17, "don_subvencion", "Don Subvención", ["lobista"], "andalucia", [85, 60, 80, 70, 75, 35], 80, 150, None, "lobista",
  [("influencia", 1), ("puerta_giratoria", 1), ("clientelismo", 1), ("maletin", 20)],
  "Sentado sobre un sobre gigante, sonríe a todo el mundo. El puro no lo enciende nunca: es decorativo."),
 (18, "concejal_aldea", "Concejal de Aldea", ["baron", "regionalista"], "galicia", [60, 45, 60, 45, 60, 30], 170, 75, (19, 30), "baron_gal",
  [("dedazo", 1), ("morrina", 1), ("hecho_diferencial", 20), ("depende", 24)],
  "Lleva boina, bastón y paraguas, por si acaso. Conoce a todos los vecinos y a todas sus vacas."),
 (19, "cacique", "Cacique Comarcal", ["baron", "regionalista"], "galicia", [90, 80, 90, 70, 85, 50], 60, 190, None, "baron_gal",
  [("dedazo", 1), ("hecho_diferencial", 1), ("vara_de_mando", 1), ("fuero_propio", 34)],
  "Su anillo pesa tanto como su influencia. Guarda los votos en un hórreo en miniatura."),
 (20, "retranqueiro", "O Retranqueiro", ["regionalista"], "galicia", [70, 55, 70, 80, 85, 70], 70, 160, None, "regionalista",
  [("depende", 1), ("morrina", 1), ("hecho_diferencial", 1), ("fuero_propio", 28)],
  "Si le preguntas si sube o baja la escalera, responde: \"Depende\". Nadie le ha visto entero: siempre hay niebla."),
 (21, "notario", "Notario do Pazo", ["leguleyo"], "galicia", [70, 45, 75, 90, 90, 35], 80, 150, None, "leguleyo",
  [("letra_pequena", 1), ("querella", 1), ("auto_de_imputacion", 1), ("recurso_constitucional", 26)],
  "Da fe de todo, incluso de lo que no ha pasado todavía. Ata sus legajos con cinta de 1890."),
 (22, "silenciosa", "A Silenciosa", ["burocrata"], "galicia", [75, 50, 95, 50, 90, 30], 90, 140, None, "burocrata",
  [("silencio_administrativo", 1), ("sello_oficial", 1), ("vuelva_usted_manana", 1), ("expediente_infinito", 1)],
  "Nunca dice nada. Su montaña de expedientes crece cada noche y nadie sabe de dónde salen."),
 (23, "mariscador", "Mariscador Mobilizado", ["activista"], "galicia", [70, 85, 70, 45, 60, 65], 90, 145, None, "activista",
  [("pancarta", 1), ("sentada", 1), ("recogida_de_firmas", 1), ("huelga_general", 26)],
  "Sale de la ría directo a la manifestación sin quitarse las botas. Sigue goteando horas después."),
 (24, "abogado", "Abogado Recurrente", ["leguleyo"], "galicia", [65, 70, 60, 85, 65, 60], 90, 150, None, "leguleyo",
  [("letra_pequena", 1), ("querella", 1), ("recurso_constitucional", 1), ("auto_de_imputacion", 28)],
  "Su escrito de alegaciones mide cuatro kilómetros y sigue creciendo. Se tropieza con él a menudo."),
 (25, "eterno", "El Eterno Candidato", ["populista", "baron"], "", [110, 100, 95, 110, 95, 90], 25, 300, None, "eterno",
  [("promesa_imposible", 1), ("discurso_encendido", 1), ("vara_de_mando", 1), ("arranque_campana", 1)],
  "Lleva en campaña desde que se inventaron las urnas. Su sonrisa de cartel no se ha movido ni un milímetro."),
]


def main():
    out = {"_doc": "Stats: carisma=PS, oratoria=ataque, aguante=defensa, dialectica=at. especial, templanza=def. especial, agilidad=velocidad."}
    for (pid, key, name, types, zone, stats, catch, exp, evo, pal, moves, desc) in P:
        d = {
            "key": key, "name": name, "types": types, "zone": zone,
            "stats": dict(zip(STATS, stats)), "catch_rate": catch, "base_exp": exp,
            "palette": pal, "moves": [[m, l] for m, l in moves], "desc": desc,
        }
        if evo:
            d["evolves_to"] = evo[0]
            d["evolve_level"] = evo[1]
        out[str(pid)] = d
    with open(os.path.join(ROOT, "data", "politicians.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print("politicians:", len(P))


if __name__ == "__main__":
    main()
