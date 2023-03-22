<template>
  <h1 class="text-3xl font-bold underline">
    Hello World!
  </h1>
  <h2>
    {{ message }}
  </h2>
</template>

<script>
import axios from 'axios';

export default {
  data() {
    return {
      message: 'foo'
    }
  },

  async created() {
    try {
      const response = await axios.get('https://httpbin.org/get');
      this.message = response.data;
    } catch (error) {
      this.message = "Error: " + error;
    }
  }
};
</script>
