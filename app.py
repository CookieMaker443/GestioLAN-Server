from flask import Flask, request, jsonify
import mysql.connector

app = Flask(__name__)

DB_CONFIG = {
    'host': 'localhost',
    'user': 'root',
    'password': 'root',
    'database': 'Gestionale_Casa'
}

def get_db_connection():
    try:
        conn = mysql.connector.connect(**DB_CONFIG)
        return conn
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        return None
    
@app.route('/oggetti', methods=['GET'])
def get_oggetti():
    db = get_db_connection()
    if db is None:
        return jsonify({'error': 'Database connection failed'}), 500
    
    try:
        cursor = db.cursor(dictionary=True)
        query = "SELECT * FROM Oggetti"

        #query="""
        #    SELECT o.id_oggetto, o.nome, o.quantita, o.descrizione, o.percorso_immagine, 
        #           c.nome_categoria 
        #    FROM Oggetti o
        #    JOIN Categorie c ON o.id_categoria = c.id_categoria
        #"""

        cursor.execute(query)
        oggetti = cursor.fetchall()
        return jsonify(oggetti), 200
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        return jsonify({'error': 'Query execution failed'}), 500
    finally:
        if db.is_connected():
            db.close()

@app.route('/oggetti', methods=['POST'])
def add_oggetto():
    db = get_db_connection()
    if db == None:
        return jsonify({'error': 'Database connection failed'}), 500
    
    data = request.get_json()
    nome = data.get('nome')
    quantita = data.get('quantita')
    descrizione = data.get('descrizione')
    percorso_immagine = data.get('percorso_immagine')
    id_categoria = data.get('id_categoria')

    try:
        cursor = db.cursor()
        query = """
            INSERT INTO Oggetti (nome, quantita, descrizione, percorso_immagine, id_categoria)
            VALUES (%s, %s, %s, %s, %s)
        """
        cursor.execute(query, (nome, quantita, descrizione, percorso_immagine, id_categoria))
        db.commit()
        return jsonify({'message': 'Oggetto added successfully'}), 201
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        return jsonify({'error': 'Failed to add Oggetto'}), 500
    finally:
        if db.is_connected():
            db.close()

@app.route('/oggetti/int:id_item/quantita', methods=['PUT'])
def update_item_quantity(id_item):
    dati = request.get_json()
    variazione = dati.get('variazione')

    if variazione is None or variazione == 0 or not isinstance(variazione, int):
        return jsonify({'error': 'Invalid variazione value'}), 400
    
    db = get_db_connection()
    if db is None:
        return jsonify({'error': 'Database connection failed'}), 500
    
    try:
        cursor = db.cursor()
        # La query usa un valore parametrizzato per la variazione e l'ID
        query = "UPDATE Oggetti SET quantita = quantita + %s WHERE id_oggetto = %s"
        cursor.execute(query, (variazione, id_item))
        db.commit()
        if cursor.rowcount == 0:
            return jsonify({'error': 'Item not found'}), 404
        return jsonify({'message': 'Quantity updated successfully'}), 200
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        return jsonify({'error': 'Failed to update quantity'}), 500
    finally:
        if db.is_connected():
            db.close()

@app.route('/oggetti/int:id_item', methods=['DELETE'])
def delete_oggetto(id_item):
    db = get_db_connection()
    if db is None:
        return jsonify({'error': 'Database connection failed'}), 500

    try:
        cursor = db.cursor()
        query = "DELETE FROM Oggetti WHERE id_oggetto = %s"
        cursor.execute(query(id_item))
        db.commit()
        if cursor.rowcount == 0:
            return jsonify({'error': 'Oggetto not found'}), 404
        return jsonify({'message': 'Oggetto deleted successfully'}), 200
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        return jsonify({'error': 'Failed to delete Oggetto'}), 500
    finally:
        if db.is_connected():
            db.close()





if __name__ == '__main__':
    #La tua API sarà in ascolto su http://127.0.0.1:5000
    app.run(host='127.0.0.1', port=5000, debug=False)