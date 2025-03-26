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

pushinka=0.001

def dist(a,b):
	c = math.sqrt((b[0] - a[0])*(b[0] - a[0])+(b[1] - a[1])*(b[1] - a[1])+(b[2] - a[2])*(b[2] - a[2]))
	return c

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
	  

opts = PromptSelectionOptions()
opts.MessageForAdding = "Объекттерди танда"
per = ed.GetSelection(opts)
if per.Status == PromptStatus.OK:

		
	print ("selected "+str(per.Value.Count))
	tr = db.TransactionManager.StartTransaction()

	points=[]
	def in_points(p):
		#print(p)
		found=False
		found_i=-1
		i=0
		for pt in points:
			d=dist(pt,p)
			if not found and d<pushinka:
				found=True
				found_i=i
			i+=1
		return found_i

        sides=[]
	def in_sides(s):
		found=False
		for sd in sides:
			if not found and ((s[0]==sd[0] and s[1]==sd[1]) or (s[0]==sd[1] and s[1]==sd[0])):
				found=True
				sd[2]=True
		return found
	layers=[]
	lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
	for ltr in lt:
		lr=tr.GetObject(ltr, OpenMode.ForRead)
		layers.append( lr.Name)

        for layer in layers:


                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Face and ent.Layer==layer:
                                p=ent.GetVertexAt(0)
				s0=in_points(p)
				#print(str(s0))
                                if s0==-1:
                                        points.append([p[0],p[1],p[2]])
					s0=len(points)-1
				#print("    "+str(s0))

                                p=ent.GetVertexAt(1)
				s1=in_points(p)
				#print(str(s1))
                                if s1==-1:
                                        points.append([p[0],p[1],p[2]])
					s1=len(points)-1
				#print("    "+str(s1))

                                p=ent.GetVertexAt(2)
				s2=in_points(p)
				#print(str(s2))
                                if s2==-1:
                                        points.append([p[0],p[1],p[2]])
					s2=len(points)-1
				#print("    "+str(s2))

                                p=ent.GetVertexAt(3)
				s3=in_points(p)
				#print(str(s3))
                                if s3==-1:
                                        points.append([p[0],p[1],p[2]])
					s3=len(points)-1
				#print("    "+str(s3))

				if not in_sides([s0,s1]):
					sides.append([s0,s1,False])
				if not in_sides([s1,s2]):
					sides.append([s1,s2,False])
				if not in_sides([s2,s3]):
					sides.append([s2,s3,False])
				if not in_sides([s3,s0]):
					sides.append([s3,s0,False])
        
	tr.Commit() 


	i=0
	for p in points:
		p0=make_point(p[0],p[1],p[2])
		#pnt=doc.ModelSpace.AddPoint(p0)	 
		#txt=doc.ModelSpace.AddText(str(i),p0,1.0)
		i+=1


	for s in sides:
		#print(s) 
		if s[2]==False: 
			p0=make_point(points[s[0]][0],points[s[0]][1],points[s[0]][2])
			p1=make_point(points[s[1]][0],points[s[1]][1],points[s[1]][2])
			ln=doc.ModelSpace.AddLine(p0,p1)	 

	#anykey=doc.Utility.GetString(1,Prompt = "\nEnter: ")


