import axios from 'axios'

// Change this if your backend runs on a different port.
// Check the terminal output when you run "dotnet run" - it prints the actual URL.
const BASE_URL = 'http://localhost/api'

const axiosInstance = axios.create({
  baseURL: BASE_URL,
})

// This runs before EVERY request made with axiosInstance.
// If we have a token saved (user is logged in), attach it as
// "Authorization: Bearer <token>" so protected endpoints accept it.
axiosInstance.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export default axiosInstance
