import clr
import System
import System.Reflection.Assembly as Assembly


from Autodesk.AutoCAD.ApplicationServices import *
from  Autodesk.AutoCAD.DatabaseServices import *
from Autodesk.AutoCAD.EditorInput import *
from  Autodesk.AutoCAD.Runtime import * 

from Autodesk.AutoCAD.Colors import *

from AutoCAD import *

from System import *
from System.Diagnostics import *
from System.Runtime.InteropServices import *

def get_acad_obj():
	aCAD =  Process.GetProcessesByName("acad")
	if len(aCAD) == 0:
		print "автокад не запущен"
		return None
	else:
		return Marshal.GetActiveObject("AutoCAD.Application")

standalone = False
# standalone = True


def interpol(n1,n2,kaisy,kancha):
	return  (n2-n1)*kaisy/kancha+n1


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


docNet = Application.DocumentManager.MdiActiveDocument
ed = docNet.Editor
db = docNet.Database
	  

tr = db.TransactionManager.StartTransaction()

lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
for ltr in lt:
	lr=tr.GetObject(ltr, OpenMode.ForRead)
	lrw=tr.GetObject(ltr, OpenMode.ForWrite)
	name=lr.Name
	doc.Utility.Prompt(name)
	splitted=name.split("_")

	if len(splitted)==3:
		doc.Utility.Prompt(str(splitted))
		if len(splitted[0])==3:
			diam=int(splitted[1])
			doc.Utility.Prompt("*"+str(diam))
			ltw.Color = Color.FromColorIndex(ColorMethod.ByAci, 3)

	doc.Utility.Prompt("\n")

tr.Commit() 

