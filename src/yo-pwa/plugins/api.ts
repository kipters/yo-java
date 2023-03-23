import axios from "axios";
import { useAuthStore } from '../stores/auth';

const getHeaders = () => {
  const authStore = useAuthStore();

  if (!authStore.token) return {}

  return {
    Authorization: `Bearer ${authStore.token}`
  }
}

export default defineNuxtPlugin(() => {
  const api = axios.create({
    baseURL: "http://localhost:5000",
    headers: getHeaders(),
  });
  return {
    provide: {
      axios: api,
    },
  };
});
