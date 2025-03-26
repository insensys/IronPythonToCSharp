#import clr
#import System
#import System.Reflection.Assembly as Assembly


#from Autodesk.AutoCAD.ApplicationServices import *
#from  Autodesk.AutoCAD.DatabaseServices import *
#from Autodesk.AutoCAD.EditorInput import *
#from  Autodesk.AutoCAD.Runtime import * 

#doc = Application.DocumentManager.MdiActiveDocument
#ed = doc.Editor
#db = doc.Database

import math


def interpol(n1,n2,kaisy,kancha):
	return  (n2-n1)*kaisy/kancha+n1


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
	  

kancha=doc.Utility.GetInteger(Prompt = "\nканчага майдалайлы?: ")


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

	starts=[]
	ends=[]

        for layer in layers:


                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Line and ent.Layer==layer:
				starts.append([ent.StartPoint[0],ent.StartPoint[1],ent.StartPoint[2]])
				ends.append([ent.EndPoint[0],ent.EndPoint[1],ent.EndPoint[2]])

        
	tr.Commit() 

for i in range(0,len(starts)):
	if kancha>1:
	
		p1=make_point(starts[i][0],starts[i][1],starts[i][2])
		p2=make_point(ends[i][0],ends[i][1],ends[i][2])
		#line=doc.ModelSpace.AddLine(p1,p2)

		
		old=p1
		point=doc.ModelSpace.AddPoint(old)

		for j in range(1,kancha+1):
			x=interpol(starts[i][0],ends[i][0],j,kancha)
			y=interpol(starts[i][1],ends[i][1],j,kancha)
			z=interpol(starts[i][2],ends[i][2],j,kancha)

			p=make_point(x,y,z)
			point=doc.ModelSpace.AddPoint(p)
			line=doc.ModelSpace.AddLine(old,p)
			old=p
