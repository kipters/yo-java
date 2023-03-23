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
import software.amazon.awssdk.services.sqs.SqsClient;
import software.amazon.awssdk.utils.AttributeMap;

@Configuration
public class AwsClientConfig {
    private Optional<String> localstackHost = Optional.empty();

    public AwsClientConfig() {
        var host = System.getenv("LOCALSTACK_HOST");

        if (host != null) {
            this.localstackHost = Optional.of(host);
        }
    }

    @Bean
    public DynamoDbClient getDynamoDbClient() {
        if (localstackHost.isEmpty()) {
            var client = DynamoDbClient.builder().build();
            return client;
        }

        var host = "http://" + localstackHost.get() + ":4566";
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

    @Bean
    public SqsClient getSqsClient() {
        if (localstackHost.isEmpty()) {
            var client = SqsClient.builder().build();
            return client;
        }

        var host = "http://" + localstackHost.get() + ":4566";
        var credentials = AwsBasicCredentials.create("FAKE_ACCESS_KEY_ID", "FAKE_SECRET_KEY");
        var credentialsProvider = StaticCredentialsProvider.create(credentials);
        var httpAttributeMap = AttributeMap.builder()
            .put(SdkHttpConfigurationOption.TRUST_ALL_CERTIFICATES, true)
            .build();

        var httpClient = new DefaultSdkHttpClientBuilder().buildWithDefaults(httpAttributeMap);

        var client = SqsClient.builder()
            .region(Region.US_EAST_1)
            .credentialsProvider(credentialsProvider)
            .httpClient(httpClient)
            .endpointOverride(URI.create(host))
            .build();

        return client;
    }
}
