<template>
  <h1 class="text-3xl font-bold underline">
    Hello World!
  </h1>
  <h2>
    {{ message }}
    <div>
      <NuxtPage />
    </div>
  </h2>
</template>

<script>
import { useAuthStore } from './stores/auth';

export default {
  data() {
    return {
      message: 'foo'
    }
  },

  async created() {
    try {
      // const { $axios } = useNuxtApp()
      // const authStore = useAuthStore();
      // authStore.setToken("token");

      // const response = await $axios.get('/env');
      // this.message = response.data;

      // if (process.browser) {
        this.message = PushManager.permissionState();
      // } else {
        // this.message = "Server";
      // }

    } catch (error) {
      this.message = "Error: " + error;
    }
  }
};
</script>
