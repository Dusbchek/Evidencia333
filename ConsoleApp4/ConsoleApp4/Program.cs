using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IO;
using System.Linq;

namespace MongoDBExample
{
    class Program
    {
        static IMongoDatabase database;
        static IMongoCollection<BsonDocument> productosCollection;
        static IMongoCollection<BsonDocument> marcasCollection;

        static void Main(string[] args)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("evidencia3");

            productosCollection = database.GetCollection<BsonDocument>("Productos");
            marcasCollection = database.GetCollection<BsonDocument>("Marcas");

            CargarDatos("productos.txt", "Productos");
            CargarDatos("marcas.txt", "Marcas");

            while (true)
            {
                Console.WriteLine("Seleccione una opción:");
                Console.WriteLine("1. Buscar productos por marca");
                Console.WriteLine("2. Buscar marca por producto");
                Console.WriteLine("3. Eliminar producto");
                Console.WriteLine("4. Modificar atributos de producto");
                Console.WriteLine("5. Buscar producto por nombre");
                Console.WriteLine("6. Buscar marca por nombre");
                Console.WriteLine("7. Salir");

                int opcion;
                if (int.TryParse(Console.ReadLine(), out opcion))
                {
                    switch (opcion)
                    {
                        case 1:
                            BuscarProductosPorMarca();
                            break;
                        case 2:
                            BuscarMarcaPorProducto();
                            break;
                        case 3:
                            EliminarProducto();
                            break;
                        case 4:
                            ModificarProducto();
                            break;
                        case 5:
                            BuscarProductoPorNombre();
                            break;
                        case 6:
                            BuscarMarcaPorNombre();
                            break;
                        case 7:
                            return;
                        default:
                            Console.WriteLine("Opción inválida. Intente de nuevo.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Opción inválida. Intente de nuevo.");
                }

                Console.WriteLine();
            }
        }

        static void CargarDatos(string filePath, string collectionName)
        {
            var lines = File.ReadAllLines(filePath);
            var collection = collectionName == "Productos" ? productosCollection : marcasCollection;

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                var document = new BsonDocument();

                if (collectionName == "Productos")
                {
                    document.Add("Nombre", parts[0]);
                    document.Add("Stock", int.Parse(parts[1]));
                    document.Add("IdMarca", parts[2]);
                }
                else if (collectionName == "Marcas")
                {
                    document.Add("Nombre", parts[0]);
                    document.Add("PaisOrigen", parts[1]);
                    document.Add("Productos", BsonArray.Create(new List<string>(parts[2].Split(';'))));
                }

                collection.InsertOne(document);
            }
        }

        static void BuscarProductosPorMarca()
        {
            Console.WriteLine("Ingrese el nombre de la marca:");
            string marca = Console.ReadLine();

            Console.WriteLine("¿Desea aplicar filtros de stock? (S/N):");
            string respuesta = Console.ReadLine().ToUpper();
            int stock = -1;

            if (respuesta == "S")
            {
                Console.WriteLine("Ingrese el stock");
                stock = int.Parse(Console.ReadLine());
            }

            var filter = Builders<BsonDocument>.Filter.Eq("Nombre", marca);
            var marcaDocument = marcasCollection.Find(filter).FirstOrDefault();

            if (marcaDocument != null)
            {
                var productos = marcaDocument.GetValue("Productos").AsBsonArray;
                Console.WriteLine($"Productos de la marca {marca}:");

                foreach (var producto in productos)
                {
                    var productoFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("Nombre", producto.AsString),
                        Builders<BsonDocument>.Filter.Gte("Stock", stock)
                    );

                    var productosMarca = productosCollection.Find(productoFilter).ToList();

                    foreach (var p in productosMarca)
                    {
                        Console.WriteLine(p);
                    }
                }
            }
            else
            {
                Console.WriteLine($"No se encontró la marca {marca}");
            }
        }

        static void BuscarMarcaPorProducto()
        {
            Console.WriteLine("Ingrese el nombre del producto:");
            string producto = Console.ReadLine();

            var filter = Builders<BsonDocument>.Filter.Eq("Productos", producto);
            var marcaDocument = marcasCollection.Find(filter).FirstOrDefault();

            if (marcaDocument != null)
            {
                var nombreMarca = marcaDocument.GetValue("Nombre").AsString;
                Console.WriteLine($"La marca del producto {producto} es: {nombreMarca}");
            }
            else
            {
                Console.WriteLine($"No se encontró la marca para el producto {producto}");
            }
        }

        static void BuscarProductoPorNombre()
        {
            Console.WriteLine("Ingrese el nombre del producto:");
            string nombreProducto = Console.ReadLine();

            var filter = Builders<BsonDocument>.Filter.Eq("Nombre", nombreProducto);
            var productoDocument = productosCollection.Find(filter).FirstOrDefault();

            if (productoDocument != null)
            {
                Console.WriteLine($"El producto {nombreProducto} fue encontrado:");
                Console.WriteLine(productoDocument);
            }
            else
            {
                Console.WriteLine($"No se encontró ningún producto con el nombre {nombreProducto}");
            }
        }

        static void BuscarMarcaPorNombre()
        {
            Console.WriteLine("Ingrese el nombre de la marca:");
            string nombreMarca = Console.ReadLine();

            var filter = Builders<BsonDocument>.Filter.Eq("Nombre", nombreMarca);
            var marcaDocument = marcasCollection.Find(filter).FirstOrDefault();

            if (marcaDocument != null)
            {
                Console.WriteLine($"La marca {nombreMarca} fue encontrada:");
                Console.WriteLine(marcaDocument);
            }
            else
            {
                Console.WriteLine($"No se encontró ninguna marca con el nombre {nombreMarca}");
            }
        }

        static void EliminarProducto()
        {
            Console.WriteLine("Ingrese el nombre del producto que desea eliminar:");
            string producto = Console.ReadLine();

            var filter = Builders<BsonDocument>.Filter.Eq("Nombre", producto);
            var productoDocument = productosCollection.Find(filter).FirstOrDefault();

            if (productoDocument != null)
            {
                var idMarca = productoDocument.GetValue("IdMarca").AsString;

                productosCollection.DeleteOne(filter);
                Console.WriteLine($"Producto {producto} eliminado correctamente.");

                var marcaFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(idMarca));
                var update = Builders<BsonDocument>.Update.Pull("Productos", producto);
                marcasCollection.UpdateOne(marcaFilter, update);
                Console.WriteLine($"Producto {producto} eliminado del array de la marca.");
            }
            else
            {
                Console.WriteLine($"No se encontró el producto {producto}");
            }
        }

        static void ModificarProducto()
        {
            Console.WriteLine("Ingrese el nombre del producto que desea modificar:");
            string producto = Console.ReadLine();

            var filter = Builders<BsonDocument>.Filter.Eq("Nombre", producto);
            var productoDocument = productosCollection.Find(filter).FirstOrDefault();

            if (productoDocument != null)
            {
                Console.WriteLine("Ingrese el nuevo nombre del producto:");
                string nuevoNombre = Console.ReadLine();

                Console.WriteLine("Ingrese el nuevo stock:");
                int nuevoStock = int.Parse(Console.ReadLine());

                var idMarca = productoDocument.GetValue("IdMarca").AsString;
                var marcaFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(idMarca));
                var marcaDocument = marcasCollection.Find(marcaFilter).FirstOrDefault();

                if (marcaDocument != null)
                {
                    var productosArray = marcaDocument.GetValue("Productos").AsBsonArray;
                    for (int i = 0; i < productosArray.Count; i++)
                    {
                        if (productosArray[i].AsString == producto)
                        {
                            productosArray[i] = nuevoNombre;
                            break;
                        }
                    }

                    var updateMarca = Builders<BsonDocument>.Update.Set("Productos", productosArray);
                    marcasCollection.UpdateOne(marcaFilter, updateMarca);
                }

                var updateProducto = Builders<BsonDocument>.Update
                    .Set("Nombre", nuevoNombre)
                    .Set("Stock", nuevoStock);

                productosCollection.UpdateOne(filter, updateProducto);
                Console.WriteLine($"Producto {producto} modificado correctamente.");
            }
            else
            {
                Console.WriteLine($"No se encontró el producto {producto}");
            }
        }
    }
}
