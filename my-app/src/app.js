import { useState, useEffect } from 'react';
import axios from 'axios';

const App = () => {
  const [topics, setTopics] = useState([]);

  useEffect(() => {
    const loadTopics = async () => {
      try {
        // Make sure your backend is running and the URL is correct
        const response = await axios.get('http://localhost:5300/api/topics');
        console.log(response.data.resource);
        setTopics(response.data.resource);
      } catch (error) {
        console.error('Error fetching topics:', error);  // Log the error
      }
    };

    loadTopics();
  }, []);

  return (
    <>
      {topics.length === 0 ? (
        <p>Loading topics...</p>
      ) : (
        topics.map((topic, i) => (
          <p key={i}>{topic.title} {topic.description}</p>
        ))
      )}
    </>
  );
}

export default App;
