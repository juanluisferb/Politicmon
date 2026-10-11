import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from art import politicians_art as pa
from art.sketch import sheet
import pixlib

pals = pixlib.load_palettes()["sprites"]
pol = json.load(open(os.path.join(pixlib.ROOT, "data", "politicians.json")))
ids = sorted(pa.ALL.keys()) if len(sys.argv) < 2 else [int(a) for a in sys.argv[1].split(",")]
cs, ps, ls = [], [], []
for i in ids:
    cs.append(pa.ALL[i]())
    ps.append(pals[pol[str(i)]["palette"]])
    ls.append("%d %s" % (i, pol[str(i)]["key"]))
out = sys.argv[2] if len(sys.argv) > 2 else "/tmp/claude-0/-home-user-Politicmon/fe757f92-f1d4-5106-a7b0-1592126af86a/scratchpad/preview.png"
sheet(cs, ps, out, cols=5, scale=4, labels=ls)
print(out)
