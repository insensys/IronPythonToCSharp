import clr
import System
import System.Reflection.Assembly as Assembly


from Autodesk.AutoCAD.ApplicationServices import *
from  Autodesk.AutoCAD.DatabaseServices import *
from Autodesk.AutoCAD.EditorInput import *
from  Autodesk.AutoCAD.Runtime import * 

doc = Application.DocumentManager.MdiActiveDocument
ed = doc.Editor
db = doc.Database
	  

opts = PromptSelectionOptions()
opts.MessageForAdding = "Объекттерди танда"
per = ed.GetSelection(opts)
if per.Status == PromptStatus.OK:

		
	my_file = open("x:\\acadgson.json", "w")
	docname=doc.Name.replace('\\','/')

	my_file.write('{"name": "'+docname+'"\n')
	my_file.write(',"name_ord": [')

	k=0
	while k<len(docname):
		if k>0:
	                my_file.write(',')
		my_file.write(str(ord(docname[k])))
		k+=1
	my_file.write(']')

	my_file.write(',"layers": [')

	print "okey, selected"
	print per.Value.Count
	tr = db.TransactionManager.StartTransaction()

        layers=[]
	lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
	for ltr in lt:
		lr=tr.GetObject(ltr, OpenMode.ForRead)
		layers.append( lr.Name)
        j=0
        for layer in layers:
                if j>0:
                      my_file.write(',')  

                my_file.write('\n{"name":"'+layer+'"')
                my_file.write(',"name_ord":[')
		k=0
		while k<len(layer):
			if k>0:
		                my_file.write(',')
	                my_file.write(str(ord(layer[k])))
			k+=1

                my_file.write(']')
                my_file.write(',"points":[')

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is DBPoint and ent.Layer==layer:
                                if i!=0:
                                        my_file.write(',')
                                my_file.write('{"position":['+str(ent.Position[0])+','+str(ent.Position[1])+','+str(ent.Position[2])+'],"handle":"'+str(ent.Handle)+'"}\n')
                                i+=1
                my_file.write(']')
                my_file.write(',"lines":[')

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Line and ent.Layer==layer:
                                if i!=0:
                                        my_file.write(',')
                                my_file.write('{')
                                my_file.write('"start":['+str(ent.StartPoint[0])+','+str(ent.StartPoint[1])+','+str(ent.StartPoint[2])+']')
                                my_file.write(',"finish":['+str(ent.EndPoint[0])+','+str(ent.EndPoint[1])+','+str(ent.EndPoint[2])+']\n')
                                my_file.write(',"handle":"'+str(ent.Handle)+'"}\n')
                                i+=1
                my_file.write(']')
                my_file.write(',"faces":[')

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Face and ent.Layer==layer:
                                if i!=0:
                                        my_file.write(',')
                                my_file.write('{"vertices":')
                                p=ent.GetVertexAt(0)
                                my_file.write('[['+str(p[0])+','+str(p[1])+','+str(p[2])+']')
                                p=ent.GetVertexAt(1)
                                my_file.write(',['+str(p[0])+','+str(p[1])+','+str(p[2])+']')
                                p=ent.GetVertexAt(2)
                                my_file.write(',['+str(p[0])+','+str(p[1])+','+str(p[2])+']')
                                p=ent.GetVertexAt(3)
                                my_file.write(',['+str(p[0])+','+str(p[1])+','+str(p[2])+']]')
                                my_file.write(',"handle":"'+str(ent.Handle)+'"}\n')
                                i+=1
                my_file.write(']')
                my_file.write(',"texts":[')

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is DBText and ent.Layer==layer:
                                if i!=0:
                                        my_file.write(',')
                                my_file.write('['+str(ent.Position[0])+','+str(ent.Position[1])+','+str(ent.Position[2]))
                                my_file.write(',"'+ent.TextString+'",[')

				k=0
				while k<len(ent.TextString):
					if k>0:
				                my_file.write(',')
			                my_file.write(str(ord(ent.TextString[k])))
					k+=1

                                my_file.write('],'+str(ent.Height)+']\n')
                                i+=1
             #                   print dir(ent)
                my_file.write(']')


                my_file.write(',"tables":[')

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Table and ent.Layer==layer:
                                if i!=0:
                                        my_file.write(',')
                                my_file.write('\n{"NumRows":'+str(ent.NumRows)+',"NumColumns":'+str(ent.NumColumns))
                                my_file.write('\n,"Position":['+str(ent.Position[0])+','+str(ent.Position[1])+','+str(ent.Position[2])+']')
                                my_file.write('\n,"Height":'+str(ent.Height)+',"Width":'+str(ent.Width)+',"Rotation":'+str(ent.Rotation))
                                my_file.write('\n,"ScaleFactors":['+str(ent.ScaleFactors[0])+','+str(ent.ScaleFactors[1])+','+str(ent.ScaleFactors[2])+']')
                                my_file.write('\n,"Handle":"'+str(ent.Handle)+'"')
                                my_file.write('\n,"data":[')

				i1=0
				while i1<ent.NumRows:
					j1=0
					while j1<ent.NumColumns:
						if i1>0 or j1>0:
		                                	my_file.write('\n,')
		                                my_file.write('"'+ent.TextString(i1,j1)+'",[')
#						print ent.TextString(i1,j1)
						k=0
						while k<len(ent.TextString(i1,j1)):
							if k>0:
				                		my_file.write(',')
					                my_file.write(str(ord(ent.TextString(i1,j1)[k])))
							k+=1
		                                my_file.write(']')
						j1+=1
					i1+=1

                                my_file.write(']}')
                                i+=1
             #                   print dir(ent)
                my_file.write(']')

                my_file.write('}')

                j+=1
        
	tr.Commit() 

	my_file.write(']}')
	my_file.close()

        print layers
	
	print docname
#print dir(doc)
