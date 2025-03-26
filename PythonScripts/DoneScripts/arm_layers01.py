import clr
from AutoCAD import *

from System import *
from System.Diagnostics import *
from System.Runtime.InteropServices import *



standalone = False
# standalone = True

def make_array(elem_type,*args):
	from System import Array,Type
	result = Array.CreateInstance(Type.GetType(elem_type),len(args))
	for i,arg in enumerate(args):
		result.SetValue(arg,i)
	return result

def make_point(*args):
	pnt = make_array("System.Double",*[0,0,0])
	for i in range(min(3,len(args))):
		pnt.SetValue(args[i],i)
	return pnt
	
if standalone:
	acadApp = get_acad_obj()
	doc = acadApp.ActiveDocument
else:
	import Autodesk.AutoCAD.ApplicationServices as ap
	doc = ap.Application.DocumentManager.MdiActiveDocument.AcadDocument
	acadApp = doc.Application

		
doc.ActiveSpace = AcActiveSpace.acModelSpace 


lrs=doc.Layers
print(lrs.Count)

diams=[]
selected=[]
for lr in lrs:
	name=lr.Name
	doc.Utility.Prompt(name)
	splitted=name.split("_")

	if len(splitted)==3:
		doc.Utility.Prompt(str(splitted))
		if len(splitted[0])==3:
			diam=int(splitted[1])
			doc.Utility.Prompt("*"+str(diam))
			selected.append([lr,diam])
			diams.append(diam)
		

	doc.Utility.Prompt("\n")

#mycolor=acadApp.GetInterfaceObject("AutoCAD.AcCmColor.16")


color=6
diams.sort()
for d in diams:
	for s in selected:
		if s[1]==d:
			doc.Utility.Prompt(str(d)+" "+s[0].Name+" "+str(color)+"\n")
			if color>0:
				s[0].TrueColor.SetRGB(255,0,0) 
				doc.Utility.Prompt(str(s[0].color)+"\n")
				doc.Utility.Prompt(str(s[0].TrueColor.ColorIndex)+"\n")
				doc.Utility.Prompt(str(dir(s[0].TrueColor))+"\n")
				#s[0].TrueColor=
				#s[0].TrueColor.ColorIndex=ACAD_COLOR.acMagenta
				color-=1
		
