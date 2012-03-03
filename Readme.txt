 __                                          
/ _\_ __   /\/\   __ _ _ __  _ __   ___ _ __ 
\ \| '_ \ /    \ / _` | '_ \| '_ \ / _ \ '__|
_\ \ |_) / /\/\ \ (_| | |_) | |_) |  __/ |   
\__/ .__/\/    \/\__,_| .__/| .__/ \___|_|   
   |_|                |_|   |_|              

   
TOC
1. Introduction
2. Quick Usage
3. RoadMap
4. Version History

1. Introduction
-------------------------

SpMapper is a simple object mapper for Sharepoint lists.
Why should you use this instead Linq for Sharepoint or any other full ORM with Sharepoint support?
Simple... You shouldn't. 

But if you want a simple access to data on a list, try it out. It's simple and (not so...) fast.


2. Quick Usage
-------------------------

	/************ Object Model **********/
	using(var site = new SPSite("http://localhost:8090"))
	using (var web = site.OpenWeb()) {
		var list = web.Lists["Mapper"];
		
		// Query Items
		var spQuery = new SPQuery();
		var items = list.Query<TestClass>(spQuery).ToList();

		//Insert Item(s)
		var newItem = new TestClass { Title="Hello world", testebool = false, testeInt = 1, testeString = "insert" };
		list.Insert(newItem);
	}

	/************ Client Object Model **********/
	var clientContext = new ClientContext("http://localhost:8090");
	var list = clientContext.Web.Lists.GetByTitle("Mapper");

	// Query Items
	var camlQuery = new CamlQuery { ViewXml =  "<View />"};
	var items = list.Query<TestClass>(camlQuery, clientContext).ToList();

	//Insert Item(s)
	var newItem = new TestClass { Title="Hello Client", testebool = false, testeInt = 1, testeString = "insert" };
	list.Insert(newItem, clientContext);

Note about Client object model:
- since the client api uses a "disconnected" model, multiple ClientContext.ExecuteQuery
may be required. Example:
	- Query: only 1 roundtrip is needed;
	- Insert: requires 2 roundtrips. 1st to build a properties map and a second to push the inserted items.

	
3. RoadMap
-------------------------

Base features are now implemented.
Better testing still needed.


4. Version History
-------------------------

v0.3
2012-03-03
- Added Insert feature
- Added Update feature
- Added Delete feature
- fixed Client Model Query (includes breaking changes!)

v0.2
2012-02-21
- Added support for Sharepoint 2010 Client object model and Sharepoint 2007
- Removed the type inspection code on each item. The map is now build only once

v0.1
2012-02-08
First public apha version.
Only supports query method and sharepoint 2010 object model