import { useState, useEffect } from 'react';
import axios from 'axios';

const App = () => {
    const [topics, setTopics] = useState([]);

    useEffect(() => {
        const loadTopics = async () => {
            try {
                const response = await axios.get('http://localhost:5300/api/topics');
                console.log(response.data); // Log the array of topics

                // Set topics directly if response.data is an array
                if (Array.isArray(response.data)) {
                    setTopics(response.data);
                } else {
                    console.error('Unexpected API response structure:', response.data);
                }
            } catch (error) {
                console.error('Error fetching topics:', error);
            }
        };

        loadTopics();
    }, []);

    return (
        <>
            {topics.length > 0 ? (
                topics.map((topic) => (
                    <p key={topic.id}>
                        {topic.title} - {topic.description}
                    </p>
                ))
            ) : (
                <p>No topics available</p>
            )}
        </>
    );
};

export default App;
