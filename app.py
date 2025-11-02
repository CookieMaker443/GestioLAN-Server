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

@app.route('/oggetti', methods=['GET'])
def get_oggetti():
    db = get_db_connection()
    if db is None:
        return jsonify({'error': 'Database connection failed'}), 500

    # Liste per costruire la clausola WHERE in modo sicuro
    where_clauses = []
    params = []
    
    # Non usiamo più filtro_id, usiamo i filtri direttamente
    dati = request.args
    nome = dati.get('nome')
    categoria_id = dati.get('categoria_id') # Rinominato per chiarezza con l'ID
    quantita = dati.get('quantita')

    try:
        # 1. COSTRUZIONE DEI FILTRI (SOLO SE SONO PRESENTI)
        
        if nome:        # filtro per nome (ricerca parziale LIKE)
            where_clauses.append("nome LIKE %s")
            params.append(f"%{nome}%") # Il valore con i wildcard

        if categoria_id:   # filtro per categoria (match esatto)
            where_clauses.append("id_categoria = %s")
            params.append(categoria_id) # Il valore del filtro

        if quantita:    # filtro per quantità (match esatto)
            where_clauses.append("quantita < %s")
            params.append(quantita) # Il valore del filtro

        # 2. ASSEMBLAGGIO DELLA QUERY
        query_base = "SELECT * FROM Oggetti"
        
        query_finale = query_base
        
        if where_clauses:
            # Unisce i frammenti SQL con " AND " e aggiunge WHERE
            query_finale += " WHERE " + " AND ".join(where_clauses)
        
        # 3. ESECUZIONE DELLA QUERY (SICURA)
        cursor = db.cursor(dictionary=True)
        
        # Passiamo la query che ora usa %s e la tupla dei parametri
        cursor.execute(query_finale, tuple(params))
        
        oggetti = cursor.fetchall()
        
        cursor.close()
        return jsonify(oggetti), 200
        
    except mysql.connector.Error as err:
        print(f"Error: {err}")
        # Restituisci anche la query_finale per debugging
        return jsonify({'error': 'Failed to retrieve Oggetti', 'query_attempted': query_finale}), 500
    except Exception as e:
        print(f"General Error: {e}")
        return jsonify({'error': 'Internal server error'}), 500
    finally:
        if db.is_connected():
            db.close()


if __name__ == '__main__':
    #La tua API sarà in ascolto su http://127.0.0.1:5000
    app.run(host='127.0.0.1', port=5000, debug=False)