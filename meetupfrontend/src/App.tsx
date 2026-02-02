import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import Home from './pages/Home';
import CreateEvent from './pages/CreateEvent';
import AddFriend from './pages/AddFriend';
import EditProfile from './pages/EditProfile';
import EditEvent from './pages/EditEvent'; 

function App() {
  return (
    <BrowserRouter>
      <Routes>
        
        <Route path="/" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/home" element={<Home />} />
        <Route path="/create-event" element={<CreateEvent />} />
        <Route path="/add-friend" element={<AddFriend />} />
        <Route path="/settings" element={<EditProfile />} />
        <Route path="/edit-event" element={<EditEvent />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App;