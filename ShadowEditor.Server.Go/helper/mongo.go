package helper

import (
	"context"
	"fmt"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

// NewMongo create new mongo from current config
func NewMongo() (*Mongo, error) {
	if Config == nil {
		return nil, fmt.Errorf("config is not initialized")
	}
	return Mongo{}.Create(Config.Database.Connection, Config.Database.Database)
}

// Mongo represent a mongo client
type Mongo struct {
	ConnectionString string
	DatabaseName     string
	Client           *mongo.Client
	Database         *mongo.Database
}

// Create create a mongo client from connectionString and databaseName
func (m Mongo) Create(connectionString, databaseName string) (mr *Mongo, err error) {
	m.ConnectionString = connectionString
	m.DatabaseName = databaseName

	clientOptions := options.Client().ApplyURI(connectionString)

	client, err := mongo.NewClient(clientOptions)
	if err != nil {
		return nil, err
	}

	ctx, cancel := context.WithTimeout(context.Background(), 20*time.Second)
	defer cancel()

	err = client.Connect(ctx)
	if err != nil {
		return nil, err
	}

	db := client.Database(databaseName)

	m.Client = client
	m.Database = db
	return &m, nil
}

// ListCollectionNames list collectionNames of database
func (m Mongo) ListCollectionNames() (collectionNames []string, err error) {
	if m.Database == nil {
		return nil, fmt.Errorf("mongo client is not created")
	}
	nameOnly := true
	listOptions := options.ListCollectionsOptions{&nameOnly}
	return m.Database.ListCollectionNames(context.TODO(), bson.M{}, &listOptions)
}

// GetCollection get a collection from collectionName
func (m Mongo) GetCollection(name string) (collection *mongo.Collection, err error) {
	if m.Database == nil {
		return nil, fmt.Errorf("mongo client is not created")
	}
	return m.Database.Collection(name), nil
}

// RunCommand run a mongo command
func (m Mongo) RunCommand(command interface{}) (result *mongo.SingleResult, err error) {
	if m.Database == nil {
		return nil, fmt.Errorf("mongo client is not created")
	}
	return m.Database.RunCommand(context.TODO(), command), nil
}

// DropCollection drop a collection
func (m Mongo) DropCollection(name string) error {
	if m.Database == nil {
		return fmt.Errorf("mongo client is not created")
	}
	return m.Database.Collection(name).Drop(context.TODO())
}

// InsertOne insert one document to a collection
func (m Mongo) InsertOne(collectionName string, document interface{}) (*mongo.InsertOneResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.InsertOne(context.TODO(), document)
}

// InsertMany insert many documents to a collection
func (m Mongo) InsertMany(collectionName string, documents []interface{}) (*mongo.InsertManyResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.InsertMany(context.TODO(), documents)
}

// Count get documents count of a collection
func (m Mongo) Count(collectionName string, filter interface{}) (int64, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return 0, err
	}
	return collection.CountDocuments(context.TODO(), filter)
}

// FindOne find one document from a collection
func (m Mongo) FindOne(collectionName string, filter interface{}) (*mongo.SingleResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.FindOne(context.TODO(), filter), nil
}

// Find find a cursor from a collection
func (m Mongo) Find(collectionName string, filter interface{}) (*mongo.Cursor, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.Find(context.TODO(), filter)
}

// FindMany find many results of a collection
func (m Mongo) FindMany(collectionName string, filter interface{}) (result []interface{}, err error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	cursor, err := collection.Find(context.TODO(), filter)
	if err != nil {
		return nil, err
	}
	defer cursor.Close(context.TODO())

	results := []interface{}{}
	err = cursor.All(context.TODO(), results)
	if err != nil {
		return nil, err
	}
	return results, nil
}

// UpdateOne update one document
func (m Mongo) UpdateOne(collectionName string, filter interface{}, update interface{}) (*mongo.UpdateResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.UpdateOne(context.TODO(), filter, update)
}

// UpdateMany update many documents
func (m Mongo) UpdateMany(collectionName string, filter interface{}, update interface{}) (*mongo.UpdateResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.UpdateMany(context.TODO(), filter, update)
}

// DeleteOne delete one document
func (m Mongo) DeleteOne(collectionName string, filter interface{}) (*mongo.DeleteResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.DeleteOne(context.TODO(), filter)
}

// DeleteMany delete many documents
func (m Mongo) DeleteMany(collectionName string, filter interface{}) (*mongo.DeleteResult, error) {
	collection, err := m.GetCollection(collectionName)
	if err != nil {
		return nil, err
	}
	return collection.DeleteMany(context.TODO(), filter)
}
