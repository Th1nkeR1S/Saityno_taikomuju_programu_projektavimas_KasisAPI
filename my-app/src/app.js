import { useState, useEffect } from 'react';
import axios from 'axios';

const App = () => {
    const [topics, setTopics] = useState([]);   // State to hold topics data
    const [error, setError] = useState(null);    // State to hold any errors
    const [loading, setLoading] = useState(true); // State to handle loading state

    useEffect(() => {
        const loadTopics = async () => {
            try {
                setLoading(true);  // Set loading to true before making the request
                const response = await axios.get('/api/topics'); // Use the proxy URL
                console.log(response.data); // Log the response to check the structure
                setTopics(response.data);  // Store the fetched data in state
            } catch (error) {
                console.error("Error loading topics:", error); // Log the error details
                setError(error.response ? error.response.data : error.message);  // Capture detailed error message
            } finally {
                setLoading(false);  // Set loading to false once the request is complete
            }
        };

        loadTopics();
    }, []);  // Empty dependency array, runs only once after the component mounts

    // Return the UI based on the states
    return (
        <div>
            {loading ? (
                <p>Loading topics...</p>  // Show loading message while data is being fetched
            ) : error ? (
                <p>Error: {error}</p>  // Show error message if any error occurs
            ) : topics.length > 0 ? (
                topics.map((topic, i) => (
                    <div key={i}>
                        <h3>{topic.title}</h3>  {/* Display topic title */}
                        <p>{topic.description}</p> {/* Display topic description */}
                    </div>
                ))
            ) : (
                <p>No topics available.</p>  // Show this if no topics were fetched
            )}
        </div>
    );
}

export default App;
