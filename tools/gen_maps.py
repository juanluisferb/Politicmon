"""Map designer: builds every map with drawing operations and exports data/maps/*.json.

Also renders docs/maps/<id>.png previews using the metatile atlas.
Each map row is a string; each char maps to a metatile through the zone legend.
"""
import json
import os
from PIL import Image
import pixlib

LEGENDS = {
    "madrid": {
        ",": "sidewalk", ".": "asphalt", "|": "road_dash_v", "-": "road_dash_h", "z": "zebra",
        "g": "grass", '"': "tall_grass", "T": "tree", "f": "flowers", "h": "hedge", "b": "bench",
        "l": "lamp", "k": "kiosk", "i": "fence_iron", "o": "fountain", "S": "sign",
        "R": "roof", "r": "roof_edge", "W": "wall", "w": "window", "D": "door",
        "M": "ministry_column", "m": "ministry_wall", "N": "ministry_door",
        "P": "sede_roof", "p": "sede_roof_edge", "Q": "sede_wall", "q": "sede_sign", "E": "sede_door",
        "A": "asamblea_roof", "a": "asamblea_roof_edge", "C": "asamblea_column", "c": "asamblea_emblem", "G": "asamblea_door",
    },
    "andalucia": {
        ",": "cobble", ".": "albero", '"': "dry_grass", "T": "naranjo", "O": "olivo", "S": "sign_albero",
        "f": "flowers", "g": "grass", "l": "farol", "k": "macetas", "i": "fence_wood", "o": "fountain",
        "R": "tile_roof", "r": "tile_roof_edge", "W": "cal_wall", "w": "cal_window", "D": "cal_door",
        "^": "caseta_top", "K": "caseta", "Y": "caseta_door", "<": "caseta2_top", ">": "caseta2",
        "P": "sede_roof", "p": "sede_roof_edge", "Q": "sede_wall", "q": "sede_sign", "E": "sede_door",
        "A": "sur_roof", "a": "sur_roof_edge", "C": "sur_column", "c": "sur_emblem", "G": "sur_door",
    },
    "galicia": {
        ",": "stone_path", ".": "prado", '"': "fog_grass", "T": "pine", "S": "sign_prado",
        "i": "stone_fence", "~": "water", "#": "dock", "B": "boat", "X": "rocks", "s": "sand",
        "H": "horreo_top", "J": "horreo_bottom", "f": "flowers", "o": "fountain",
        "R": "slate_roof", "r": "slate_roof_edge", "W": "granite_wall", "w": "granite_window", "D": "granite_door",
        "P": "sede_roof", "p": "sede_roof_edge", "Q": "sede_wall", "q": "sede_sign", "E": "sede_door",
        "A": "noroeste_roof", "a": "noroeste_roof_edge", "C": "noroeste_column", "c": "noroeste_emblem", "G": "noroeste_door",
    },
    "interior": {
        "#": "wall_int", "_": "floor", "t": "tile_floor", "=": "counter", "X": "carpet", "x": "carpet_plain",
        "s": "seat", "p": "plant", "e": "mat", "B": "shelf", "U": "urna", "Y": "podium", "K": "desk", " ": "void",
    },
}

BUILDINGS = {
    "house": ["RRRRRR", "rrrrrr", "wWWwWw", "WWDWWW"],
    "house_b": ["RRRRRR", "rrrrrr", "wWwwWw", "WWWwWW"],
    "small_house": ["RRRRR", "rrrrr", "wWwWw", "WWDWW"],
    "sede": ["PPPPPP", "pppppp", "QqQQqQ", "QQEQQQ"],
    "gym": ["AAAAAA", "aaaaaa", "CcCCcC", "CCGCCC"],
    "hemiciclo": ["RRRRRR", "rrrrrr", "MmMMmM", "MMNMMM"],
    "ministry": ["RRRRRR", "rrrrrr", "MmMMmM", "MmMMmM"],
    "caseta": ["^^^^", "KYKK"],
    "caseta2": ["<<<<", ">>>>"],
    "horreo": ["H", "J"],
}
DOOR_CHARS = set("DEGN")


class Map:
    def __init__(self, mid, name, zone, legend, w, h, fill, music, border):
        self.id = mid
        self.name = name
        self.zone = zone
        self.legend = legend
        self.w, self.h = w, h
        self.grid = [[fill] * w for _ in range(h)]
        self.music = music
        self.border = border
        self.npcs, self.warps, self.signs, self.items, self.areas = [], [], [], [], []
        self.encounters = None
        self.doors = {}
        self.interior = legend == "interior"

    def put(self, x, y, ch):
        assert 0 <= x < self.w and 0 <= y < self.h, (self.id, x, y)
        self.grid[y][x] = ch

    def fill(self, x0, y0, x1, y1, ch):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, ch)

    def frame(self, ch):
        self.fill(0, 0, self.w - 1, 0, ch)
        self.fill(0, self.h - 1, self.w - 1, self.h - 1, ch)
        self.fill(0, 0, 0, self.h - 1, ch)
        self.fill(self.w - 1, 0, self.w - 1, self.h - 1, ch)

    def stamp(self, x, y, rows):
        for j, r in enumerate(rows):
            for i, ch in enumerate(r):
                if ch != " ":
                    self.put(x + i, y + j, ch)

    def building(self, kind, x, y, key=None):
        rows = BUILDINGS[kind]
        self.stamp(x, y, rows)
        for j, r in enumerate(rows):
            for i, ch in enumerate(r):
                if ch in DOOR_CHARS or ch == "Y":
                    if key:
                        self.doors[key] = (x + i, y + j)
        return self.doors.get(key)

    def npc(self, **kw):
        self.npcs.append(kw)

    def warp(self, x, y, to, tx, ty, d="down", **kw):
        w = {"x": x, "y": y, "to": to, "tx": tx, "ty": ty, "dir": d}
        w.update(kw)
        self.warps.append(w)

    def sign(self, x, y, text):
        self.signs.append({"x": x, "y": y, "text": text})

    def item(self, iid, x, y, item, count=1):
        self.items.append({"id": iid, "x": x, "y": y, "item": item, "count": count})

    def area(self, name, x, y, w, h):
        self.areas.append({"name": name, "rect": [x, y, w, h]})

    def to_json(self):
        legend = LEGENDS[self.legend]
        used = sorted({ch for r in self.grid for ch in r})
        for ch in used:
            assert ch in legend, (self.id, repr(ch))
        return {
            "id": self.id, "name": self.name, "zone": self.zone, "music": self.music,
            "width": self.w, "height": self.h, "border": self.border, "interior": self.interior,
            "legend": {ch: legend[ch] for ch in used + ([self.border] if self.border not in used else [])},
            "rows": ["".join(r) for r in self.grid],
            "warps": self.warps, "npcs": self.npcs, "signs": self.signs, "items": self.items,
            "areas": self.areas, "encounters": self.encounters,
        }


# ------------------------------------------------------------------ Madrid
def madrid():
    m = Map("madrid", "Madrid", "madrid", "madrid", 24, 46, ",", "madrid", "T")
    m.frame("T")
    # north: Ministerios plaza
    m.building("gym", 2, 2, "gym")
    m.building("hemiciclo", 16, 2, "hemiciclo")
    m.put(11, 4, "o")
    m.put(10, 4, "f")
    m.put(12, 4, "f")
    m.put(11, 3, "f")
    m.put(9, 5, "l")
    m.put(14, 5, "l")
    m.put(10, 7, "b")
    m.put(13, 7, "b")
    m.put(8, 5, "S")
    m.put(15, 5, "S")
    m.put(0, 8, ",")
    m.fill(1, 10, 7, 10, "h")
    m.fill(15, 10, 22, 10, "h")
    # Gran Via
    m.fill(10, 11, 12, 41, ".")
    m.fill(11, 11, 11, 41, "|")
    m.building("house_b", 2, 11)
    m.building("sede", 15, 11, "sede")
    m.building("house", 2, 17, "closed1")
    m.building("ministry", 15, 17)
    m.building("house_b", 2, 23)
    m.building("house", 15, 23, "closed2")
    for y in (13, 19, 25):
        m.put(9, y, "l")
        m.put(13, y, "l")
    m.put(22, 14, "k")
    m.put(22, 20, "k")
    m.put(22, 26, "k")
    m.put(1, 15, "b")
    m.put(1, 21, "b")
    m.fill(10, 22, 12, 22, "z")
    m.fill(10, 28, 12, 28, "z")
    m.put(8, 22, "S")
    # Barrio de Tertulias
    m.fill(1, 29, 8, 41, "g")
    m.fill(2, 31, 7, 38, '"')
    m.fill(1, 29, 7, 29, "i")
    m.put(8, 29, ",")
    m.fill(9, 29, 9, 41, ",")
    m.put(1, 40, "T")
    m.put(1, 30, "T")
    m.put(7, 40, "f")
    m.put(4, 40, "b")
    m.fill(14, 29, 22, 41, "g")
    m.fill(13, 29, 13, 41, ",")
    m.fill(15, 30, 21, 33, '"')
    m.fill(14, 29, 22, 29, "i")
    m.put(14, 29, ",")
    m.building("house", 16, 35, "garaje")
    m.fill(14, 39, 22, 41, "g")
    m.fill(16, 39, 18, 41, ",")
    m.put(22, 34, "T")
    m.put(14, 36, "T")
    m.put(14, 38, "f")
    m.put(21, 40, "f")
    m.put(13, 31, "S")
    # south exit gate
    m.fill(1, 42, 22, 44, "h")
    m.fill(10, 42, 12, 42, ".")
    m.put(11, 42, "|")
    m.put(11, 43, "|")
    m.put(11, 44, "|")
    m.put(11, 45, "|")
    m.put(9, 41, ",")
    m.area("Ministerios", 0, 0, 24, 10)
    m.area("Gran Vía", 0, 10, 24, 19)
    m.area("Barrio de Tertulias", 0, 29, 24, 17)
    gx, gy = m.doors["gym"]
    m.warp(gx, gy, "gym_madrid", 4, 12, "up")
    hx, hy = m.doors["hemiciclo"]
    m.warp(hx, hy, "hemiciclo", 4, 10, "up", require_badges=3, deny="Las puertas del Hemiciclo Nacional están cerradas. Un cartel dice: «Solo para quien tenga tres Escaños».")
    sx, sy = m.doors["sede"]
    m.warp(sx, sy, "sede_madrid", 4, 6, "up")
    gx2, gy2 = m.doors["garaje"]
    m.warp(gx2, gy2, "madrid_garaje", 3, 5, "up")
    m.warp(0, 8, "galicia", 24, 20, "left")
    m.warp(11, 45, "andalucia", 12, 1, "down")
    for k in ("closed1", "closed2"):
        x, y = m.doors[k]
        m.sign(x, y, "Está cerrado. Un cartel dice: «Vuelva usted mañana».")
    m.sign(8, 5, "ASAMBLEA DE MADRID. Líder: Paloma Prime Time. «Aquí se debate en directo».")
    m.sign(15, 5, "HEMICICLO NACIONAL. Abierto solo a quien gane los tres Escaños.")
    m.sign(8, 22, "GRAN VÍA. Más carriles que propuestas. Al sur, el Barrio de Tertulias.")
    m.sign(13, 31, "BARRIO DE TERTULIAS. Aquí se opina gratis. Cuidado con la hierba alta: está llena de políticos.")
    m.npc(id="guard_south", x=11, y=43, sprite="guard", dir="up", event="talk", hide_if="badge:madrid",
          text=["¡Alto! La carretera a Andalucía está cortada por una caravana de coches oficiales.",
                "Vuelve cuando tengas el Escaño de Madrid. Lo gana quien vence en la Asamblea, al norte."])
    m.npc(id="guard_west", x=1, y=8, sprite="guard", dir="right", event="talk", hide_if="badge:andalucia",
          text=["La autovía a Galicia está en obras desde hace once legislaturas.",
                "Dicen que la inaugurarán cuando alguien gane el Escaño del Sur. Pásate entonces."])
    m.npc(id="t_mad_1", x=13, y=20, sprite="rival_ppi", dir="left", trainer="t_mad_1", sight=3)
    m.npc(id="t_mad_2", x=8, y=34, sprite="rival_asc", dir="right", trainer="t_mad_2", sight=2)
    m.npc(id="mad_npc1", x=6, y=7, sprite="npc_a", dir="down", event="talk",
          text=["Los Tuiteros y los Tecnócratas tumban a los Mediáticos.",
                "¡Y la líder de la Asamblea es Mediática hasta la médula! Busca afiliados en la hierba del Barrio de Tertulias."])
    m.npc(id="mad_npc2", x=20, y=7, sprite="npc_b", dir="left", event="talk",
          text=["Dicen que dentro del Hemiciclo duerme un candidato que lleva toda la vida presentándose...",
                "Si consigues los tres Escaños, quizá te deje verle la sonrisa."])
    m.npc(id="mad_npc3", x=14, y=15, sprite="npc_c", dir="down", event="talk",
          text=["En la Sede del Partido curan a tus afiliados gratis. A cambio, solo piden una foto para redes."])
    m.npc(id="mad_npc4", x=5, y=39, sprite="npc_a", dir="right", event="talk",
          text=["En este barrio todos opinamos de todo. Yo controlo de política, fútbol y vulcanología.",
                "Consejo gratis: si un político está bajo de Carisma o envuelto en un Escándalo, se afilia más fácil."])
    m.npc(id="mad_npc5", x=21, y=24, sprite="npc_b", dir="left", event="talk",
          text=["El Puertagiratoria sale de un ministerio y entra en un consejo de administración sin despeinarse."])
    m.item("mad_item1", 3, 37, "carnet_premium", 2)
    m.item("mad_item2", 21, 33, "mitin", 1)
    m.item("mad_item3", 22, 1, "encuesta", 2)
    m.encounters = {"rate": 0.11, "table": [[8, 3, 6, 28], [4, 3, 6, 24], [9, 4, 7, 16], [1, 3, 5, 10], [6, 5, 7, 14], [7, 5, 8, 8]]}
    return m


def interior_sede(mid, zone, out_map, ox, oy, stock):
    m = Map(mid, "Sede del Partido", zone, "interior", 10, 8, "t", "sede", " ")
    m.stamp(0, 0, [
        "##########",
        "#BttttttB#",
        "#===tt===#",
        "#tttttttU#",
        "#tttttttt#",
        "#tttttttt#",
        "#pttttttp#",
        "####ee####",
    ])
    m.put(4, 2, "=")
    m.put(5, 2, "=")
    m.warp(4, 7, out_map, ox, oy + 1, "down")
    m.warp(5, 7, out_map, ox, oy + 1, "down")
    m.npc(id=mid + "_nurse", x=3, y=1, sprite="sede", dir="down", event="heal")
    m.npc(id=mid + "_clerk", x=6, y=1, sprite="clerk", dir="down", event="shop", stock=stock)
    m.sign(8, 3, "PC")
    m.npc(id=mid + "_npc", x=2, y=5, sprite="npc_c", dir="right", event="talk",
          text=["La urna de la esquina es la Secretaría de Organización: ahí gestionas a los afiliados que no caben en tu equipo."])
    return m


def interior_gym(mid, zone, out_map, ox, oy, leader, trainer, trainer_sprite, music="gym"):
    m = Map(mid, {"madrid": "Asamblea de Madrid", "andalucia": "Parlamento del Sur", "galicia": "Parlamento del Noroeste"}[zone],
            zone, "interior", 10, 14, "X", music, " ")
    m.stamp(0, 0, [
        "##########",
        "#pXXYXXXp#",
        "#XXXXXXXX#",
        "#XXXXXXXX#",
        "#sssXXsss#",
        "#XXXXXXXX#",
        "#sssXXsss#",
        "#XXXXXXXX#",
        "#sssXXsss#",
        "#XXXXXXXX#",
        "#sssXXsss#",
        "#XXXXXXXX#",
        "#pXXXXXXp#",
        "####ee####",
    ])
    m.warp(4, 13, out_map, ox, oy + 1, "down")
    m.warp(5, 13, out_map, ox, oy + 1, "down")
    m.npc(id=leader, x=4, y=2, sprite=leader, dir="down", trainer=leader, sight=0)
    m.npc(id=trainer, x=5, y=7, sprite=trainer_sprite, dir="left", trainer=trainer, sight=1)
    return m


def garaje():
    m = Map("madrid_garaje", "Garaje fundacional", "madrid", "interior", 8, 7, "_", "garaje", " ")
    m.stamp(0, 0, [
        "########",
        "#B_KK_p#",
        "#______#",
        "#______#",
        "#______#",
        "#______#",
        "###ee###",
    ])
    gx, gy = 18, 38
    m.warp(3, 6, "madrid", gx, gy + 1, "down", require_flag="has_starter", deny="Profesora Urna: «¡Espera! No puedes salir a la calle sin tu primer afiliado.»")
    m.warp(4, 6, "madrid", gx, gy + 1, "down", require_flag="has_starter", deny="Profesora Urna: «¡Espera! No puedes salir a la calle sin tu primer afiliado.»")
    m.npc(id="mentor", x=4, y=2, sprite="mentor", dir="down", event="starter")
    m.sign(3, 1, "Un escritorio con un cartel: «Partido Nuevo. Sede central (provisional)».")
    m.sign(4, 1, "Un escritorio con un cartel: «Partido Nuevo. Sede central (provisional)».")
    m.sign(1, 1, "Libros: «Cómo fundar un partido en 10 pasos», «Mil promesas para el invierno».")
    return m


def hemiciclo():
    m = Map("hemiciclo", "Hemiciclo Nacional", "madrid", "interior", 10, 12, "X", "legend", " ")
    m.stamp(0, 0, [
        "##########",
        "#pssYsssp#",
        "#XXXXXXXX#",
        "#sXXXXXXs#",
        "#ssXXXXss#",
        "#XXXXXXXX#",
        "#ssXXXXss#",
        "#XXXXXXXX#",
        "#ssXXXXss#",
        "#XXXXXXXX#",
        "#pXXXXXXp#",
        "####ee####",
    ])
    hx, hy = 18, 5
    m.warp(4, 11, "madrid", hx, hy + 1, "down")
    m.warp(5, 11, "madrid", hx, hy + 1, "down")
    m.npc(id="eterno", x=4, y=2, sprite="eterno", dir="down", event="legend", hide_if="flag:legend_done")
    return m


# ------------------------------------------------------------------ Andalucia
def andalucia():
    m = Map("andalucia", "Andalucía", "andalucia", "andalucia", 26, 40, ".", "andalucia", "T")
    m.frame("T")
    m.put(12, 0, ".")
    # Real de la Feria
    for (x, y, k) in ((2, 3, "caseta"), (7, 3, "caseta2"), (16, 3, "caseta"), (21, 3, "caseta2"),
                      (2, 8, "caseta2"), (7, 8, "caseta"), (16, 8, "caseta2"), (21, 8, "caseta")):
        m.building(k, x, y)
    for y in (2, 5, 7, 10):
        m.put(10, y, "l")
        m.put(14, y, "l")
    m.put(1, 1, "k")
    m.put(24, 1, "k")
    m.put(13, 1, "S")
    # boundary feria / south
    m.fill(1, 12, 24, 12, "k")
    m.fill(11, 12, 13, 12, ".")
    m.put(5, 12, ".")
    m.put(19, 12, ".")
    # Cortijo (west)
    m.fill(1, 13, 11, 38, ".")
    m.fill(1, 14, 10, 14, "i")
    m.put(5, 14, ".")
    m.fill(2, 16, 9, 22, '"')
    m.fill(2, 26, 7, 30, '"')
    for (x, y) in ((1, 16), (10, 17), (3, 24), (8, 24), (10, 27), (1, 31), (9, 31), (5, 23)):
        m.put(x, y, "O")
    m.building("small_house", 2, 33, "cortijo")
    m.fill(8, 32, 10, 37, '"')
    m.put(11, 20, "S")
    # Plaza del Ayuntamiento (east)
    m.fill(14, 13, 24, 38, ",")
    m.fill(12, 13, 13, 38, ".")
    m.building("sede", 15, 14, "sede")
    m.put(22, 15, "k")
    m.put(23, 15, "k")
    for x in (14, 24):
        for y in (20, 24, 28):
            m.put(x, y, "T")
    m.put(19, 24, "o")
    m.put(18, 24, "f")
    m.put(20, 24, "f")
    m.put(19, 23, "k")
    m.put(19, 25, "k")
    m.building("gym", 17, 31, "gym")
    m.put(16, 34, "l")
    m.put(23, 34, "l")
    m.put(16, 29, "S")
    m.fill(14, 37, 24, 38, "T")
    m.fill(12, 37, 13, 38, "T")
    m.fill(1, 38, 11, 38, "T")
    m.area("Real de la Feria", 0, 0, 26, 13)
    m.area("Cortijo", 0, 13, 12, 27)
    m.area("Plaza del Ayuntamiento", 12, 13, 14, 27)
    m.warp(12, 0, "madrid", 11, 44, "up")
    x, y = m.doors["sede"]
    m.warp(x, y, "sede_andalucia", 4, 6, "up")
    x, y = m.doors["gym"]
    m.warp(x, y, "gym_andalucia", 4, 12, "up")
    x, y = m.doors["cortijo"]
    m.sign(x, y, "La puerta del cortijo está cerrada. Dentro se oye: «¡Que no, que este año tampoco hay subvención!»")
    m.sign(13, 1, "REAL DE LA FERIA. Cada caseta, un partido; cada partido, una caseta.")
    m.sign(11, 20, "EL CORTIJO. Prohibido el paso a coches oficiales. Los jornaleros no muerden (casi nunca).")
    m.sign(16, 29, "PARLAMENTO DEL SUR. Líder: Don Gumersindo de la Vara. Especialista en Barones.")
    m.npc(id="t_and_1", x=12, y=9, sprite="rival_pas", dir="down", trainer="t_and_1", sight=3)
    m.npc(id="t_and_2", x=5, y=25, sprite="rival_asc", dir="right", trainer="t_and_2", sight=4)
    m.npc(id="and_npc1", x=18, y=2, sprite="npc_c", dir="down", event="talk",
          text=["¡Bienvenido a la feria! Aquí hasta los Barones bailan sevillanas.",
                "Truco: los Activistas tumban a los Barones, y los Lobistas también. Lo sé porque me lo dijo un Lobista."])
    m.npc(id="and_npc2", x=20, y=27, sprite="npc_a", dir="up", event="talk",
          text=["Don Gumersindo lleva tantos años en el Parlamento que su sillón tiene su forma.",
                "Sus afiliados son Barones de toda la vida. Los Burócratas les hacen cosquillas."])
    m.npc(id="and_npc3", x=6, y=32, sprite="npc_c", dir="down", event="talk",
          text=["En el campo hay Jornaleros Indignados. Si les das un Carnet... a lo mejor se indignan contigo."])
    m.item("and_item1", 9, 36, "puesto_lista", 1)
    m.item("and_item2", 23, 6, "factcheck", 2)
    m.item("and_item3", 2, 21, "carnet_premium", 2)
    m.encounters = {"rate": 0.11, "table": [[10, 12, 15, 24], [15, 12, 15, 24], [13, 13, 16, 18], [17, 13, 16, 14], [11, 15, 17, 10], [14, 15, 17, 10]]}
    return m


# ------------------------------------------------------------------ Galicia
def galicia():
    m = Map("galicia", "Galicia", "galicia", "galicia", 26, 40, ".", "galicia", "T")
    m.frame("T")
    m.put(25, 20, ",")
    # Pazo (north-west)
    m.fill(1, 1, 13, 13, ".")
    m.fill(1, 14, 12, 14, "i")
    m.fill(13, 1, 13, 14, "i")
    m.put(6, 14, ",")
    m.building("gym", 4, 3, "gym")
    m.fill(6, 7, 6, 15, ",")
    m.put(2, 2, "T")
    m.put(11, 2, "T")
    m.put(2, 9, "T")
    m.put(11, 9, "T")
    m.put(3, 11, "f")
    m.put(10, 11, "f")
    m.put(7, 8, "S")
    # Aldea (east)
    m.fill(14, 19, 24, 21, ",")
    m.fill(18, 8, 19, 36, ",")
    m.building("sede", 20, 12, "sede")
    m.building("horreo", 15, 13)
    m.building("horreo", 16, 13)
    m.building("small_house", 20, 23)
    for (x, y) in ((15, 9), (16, 9), (22, 9), (15, 25), (16, 25)):
        m.building("horreo", x, y)
    m.fill(20, 28, 24, 34, '"')
    m.fill(14, 28, 16, 33, '"')
    m.put(17, 22, "S")
    m.put(24, 4, "T")
    m.put(21, 4, "T")
    m.fill(14, 2, 24, 6, '"')
    m.fill(14, 1, 24, 1, "T")
    # Fog moor between areas
    m.fill(2, 16, 12, 24, '"')
    m.fill(1, 15, 17, 15, ".")
    m.fill(6, 15, 6, 25, ",")
    m.fill(7, 20, 17, 20, ",")
    for (x, y) in ((3, 18), (10, 17), (9, 23), (4, 22)):
        m.put(x, y, "T")
    # Puerto (south-west)
    m.fill(1, 26, 13, 38, "s")
    m.fill(1, 31, 13, 38, "~")
    m.fill(6, 26, 6, 30, ",")
    m.fill(5, 30, 7, 36, "#")
    m.fill(8, 33, 11, 33, "#")
    m.put(2, 34, "B")
    m.put(10, 36, "B")
    m.put(12, 31, "X")
    m.put(1, 31, "X")
    m.put(3, 37, "X")
    m.put(9, 27, "S")
    m.fill(14, 37, 24, 38, "T")
    m.fill(13, 30, 13, 38, "X")
    m.area("Pazo", 0, 0, 14, 15)
    m.area("Aldea con niebla", 14, 0, 12, 40)
    m.area("Puerto", 0, 25, 14, 15)
    m.warp(25, 20, "madrid", 1, 8, "right")
    x, y = m.doors["sede"]
    m.warp(x, y, "sede_galicia", 4, 6, "up")
    x, y = m.doors["gym"]
    m.warp(x, y, "gym_galicia", 4, 12, "up")
    m.sign(7, 8, "PARLAMENTO DO NOROESTE (en el Pazo). Líder: Dona Brétema. Especialista en Regionalistas.")
    m.sign(17, 22, "ALDEA. Aquí la niebla nunca se va. Los políticos tampoco.")
    m.sign(9, 27, "PUERTO. Prohibido mariscar sin carnet. Y sin pancarta.")
    m.npc(id="t_gal_1", x=7, y=33, sprite="rival_myt", dir="left", trainer="t_gal_1", sight=2)
    m.npc(id="t_gal_2", x=18, y=17, sprite="rival_pas", dir="down", trainer="t_gal_2", sight=2)
    m.npc(id="gal_npc1", x=23, y=19, sprite="npc_d", dir="left", event="talk",
          text=["¿Que si llueve? Depende.", "¿Que si el Parlamento está lejos? Depende. Está al noroeste, en el Pazo."])
    m.npc(id="gal_npc2", x=15, y=22, sprite="npc_d", dir="down", event="talk",
          text=["Aquí los Regionalistas juegan en casa: pegan más fuerte y aguantan más.",
                "Y la tabla de tipos no les afecta. Ni para bien ni para mal. Como el tiempo."])
    m.npc(id="gal_npc3", x=4, y=27, sprite="npc_c", dir="right", event="talk",
          text=["Los Leguleyos lo recurren todo. Si te Imputan, tu afiliado a veces ni se mueve.",
                "Ve a la Sede a que te lo arreglen: allí los abogados son baratos."])
    m.item("gal_item1", 1, 26, "puesto_lista", 1)
    m.item("gal_item2", 12, 16, "mitin", 2)
    m.item("gal_item3", 24, 34, "puesto_lista", 1)
    m.encounters = {"rate": 0.11, "table": [[18, 21, 24, 24], [20, 22, 25, 16], [21, 22, 25, 15], [22, 22, 25, 15], [23, 22, 25, 15], [24, 23, 26, 15]]}
    return m


def all_maps():
    mad = madrid()
    andl = andalucia()
    gal = galicia()
    maps = [mad, andl, gal, garaje(), hemiciclo()]
    stock_m = ["carnet_normal", "mitin", "factcheck", "encuesta"]
    stock_a = stock_m + ["carnet_premium"]
    stock_g = stock_a + ["puesto_lista"]
    sx, sy = mad.doors["sede"]
    maps.append(interior_sede("sede_madrid", "madrid", "madrid", sx, sy, stock_m))
    sx, sy = andl.doors["sede"]
    maps.append(interior_sede("sede_andalucia", "andalucia", "andalucia", sx, sy, stock_a))
    sx, sy = gal.doors["sede"]
    maps.append(interior_sede("sede_galicia", "galicia", "galicia", sx, sy, stock_g))
    gx, gy = mad.doors["gym"]
    maps.append(interior_gym("gym_madrid", "madrid", "madrid", gx, gy, "leader_madrid", "t_mad_3", "rival_myt"))
    gx, gy = andl.doors["gym"]
    maps.append(interior_gym("gym_andalucia", "andalucia", "andalucia", gx, gy, "leader_andalucia", "t_and_3", "rival_ppi"))
    gx, gy = gal.doors["gym"]
    maps.append(interior_gym("gym_galicia", "galicia", "galicia", gx, gy, "leader_galicia", "t_gal_3", "rival_asc"))
    return maps


def render(mj, atlas, tiles):
    img = Image.new("RGBA", (mj["width"] * 16, mj["height"] * 16))
    cols = tiles["_columns"]
    for y, row in enumerate(mj["rows"]):
        for x, ch in enumerate(row):
            idx = tiles[mj["legend"][ch]]["index"]
            tile = atlas.crop(((idx % cols) * 16, (idx // cols) * 16, (idx % cols) * 16 + 16, (idx // cols) * 16 + 16))
            img.paste(tile, (x * 16, y * 16))
    for n in mj["npcs"]:
        p = os.path.join(pixlib.ROOT, "assets", "sprites", "overworld", n["sprite"] + ".png")
        if os.path.exists(p):
            sheet = Image.open(p).convert("RGBA")
            row = {"down": 0, "up": 1, "left": 2, "right": 3}[n.get("dir", "down")]
            img.alpha_composite(sheet.crop((0, row * 16, 16, row * 16 + 16)), (n["x"] * 16, n["y"] * 16))
    for it in mj["items"]:
        ic = Image.open(os.path.join(pixlib.ROOT, "assets", "sprites", "items", "carnet.png")).convert("RGBA")
        img.alpha_composite(ic, (it["x"] * 16, it["y"] * 16))
    return img


def main():
    tiles = json.load(open(os.path.join(pixlib.ROOT, "data", "tiles.json")))
    atlas = Image.open(os.path.join(pixlib.ROOT, "assets", "tiles", "metatiles.png")).convert("RGBA")
    os.makedirs(os.path.join(pixlib.ROOT, "data", "maps"), exist_ok=True)
    os.makedirs(os.path.join(pixlib.ROOT, "docs", "maps"), exist_ok=True)
    ids = set()
    maps = all_maps()
    for m in maps:
        ids.add(m.id)
    for m in maps:
        mj = m.to_json()
        for w in mj["warps"]:
            assert w["to"] in ids, (m.id, w)
        with open(os.path.join(pixlib.ROOT, "data", "maps", m.id + ".json"), "w", encoding="utf-8") as f:
            json.dump(mj, f, ensure_ascii=False, indent=1)
        render(mj, atlas, tiles).save(os.path.join(pixlib.ROOT, "docs", "maps", m.id + ".png"))
    print("maps:", len(maps))


if __name__ == "__main__":
    main()
