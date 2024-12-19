import { useState, useEffect } from "react";
import axios from "axios";

const App = () => {
  const [topics, setTopics] = useState([]);

  useEffect(() => {
    const loadTopics = async () => {
      const response = await axios.get("http://localhost:5097/topics");
      setTopics(response.data.resource);
    };

    loadTopics();
  }, []);

  return (
    <> 
    {topics.map((topic, i) => (
    <p key={i}>{topic.resource.title} {topic.resource.description}</p>  
    ))}
    
    
     </>
  )
}

export default App;