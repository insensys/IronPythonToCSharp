import clr
import System
import System.Reflection.Assembly as Assembly


from Autodesk.AutoCAD.ApplicationServices import *
from  Autodesk.AutoCAD.DatabaseServices import *
from Autodesk.AutoCAD.EditorInput import *
from  Autodesk.AutoCAD.Runtime import * 


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
	  
while (1==1):
  opts = PromptSelectionOptions()
  opts.MessageForAdding = "Объекттерди танда"


  per = ed.GetSelection(opts)
  if per.Status == PromptStatus.OK:
	print "okey, selected"
	print per.Value.Count
	tr = db.TransactionManager.StartTransaction()

	points=[]
        layers=[]
	lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
	for ltr in lt:
		lr=tr.GetObject(ltr, OpenMode.ForRead)
		layers.append( lr.Name)

        for layer in layers:

                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is DBPoint and ent.Layer==layer:
				points.append([ent.Position[0],ent.Position[1],ent.Position[2]])


                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Line and ent.Layer==layer:
				points.append([ent.StartPoint[0],ent.StartPoint[1],ent.StartPoint[2]])
				points.append([ent.EndPoint[0],ent.EndPoint[1],ent.EndPoint[2]])

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Face and ent.Layer==layer:
                                p=ent.GetVertexAt(0)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(1)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(2)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(3)

				points.append([p[0],p[1],p[2]])
        
	tr.Commit()
	for p in points:
		p0=make_point(p[0],p[1],p[2])
		pnt=doc.ModelSpace.AddPoint(p0)	 



  anykey=doc.Utility.GetString(1,Prompt = "\nEnter: ")
