package dev.kipters.yodemo.config;

import java.net.URI;
import java.util.Optional;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import software.amazon.awssdk.auth.credentials.AwsBasicCredentials;
import software.amazon.awssdk.auth.credentials.StaticCredentialsProvider;
import software.amazon.awssdk.core.internal.http.loader.DefaultSdkHttpClientBuilder;
import software.amazon.awssdk.http.SdkHttpConfigurationOption;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.utils.AttributeMap;

@Configuration
public class AwsClientConfig {
    private Optional<String> localstacHost = Optional.empty();
    
    public AwsClientConfig() {
        var host = System.getenv("LOCALSTACK_HOST");

        if (host != null) {
            this.localstacHost = Optional.of(host);
        }
    }

    @Bean
    public DynamoDbClient getDynamoDbClient() {
        if (localstacHost.isEmpty()) {
            var client = DynamoDbClient.builder().build();
            return client;
        }

        var host = "http://" + localstacHost.get() + ":4566";
        var credentials = AwsBasicCredentials.create("FAKE_ACCESS_KEY_ID", "FAKE_SECRET_KEY");
        var credentialsProvider = StaticCredentialsProvider.create(credentials);
        var httpAttributeMap = AttributeMap.builder()
            .put(SdkHttpConfigurationOption.TRUST_ALL_CERTIFICATES, true)
            .build();

        var httpClient = new DefaultSdkHttpClientBuilder().buildWithDefaults(httpAttributeMap);

        var client = DynamoDbClient.builder()
            .region(Region.US_EAST_1)
            .credentialsProvider(credentialsProvider)
            .httpClient(httpClient)
            .endpointOverride(URI.create(host))
            .build();

        return client;
    }
}
