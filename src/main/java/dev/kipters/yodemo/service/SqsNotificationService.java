package dev.kipters.yodemo.service;

import java.util.Map;
import java.util.Optional;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import com.google.gson.Gson;

import dev.kipters.yodemo.model.NotificationModel;
import io.opentelemetry.api.trace.Span;
import software.amazon.awssdk.services.sqs.SqsClient;
import software.amazon.awssdk.services.sqs.model.MessageAttributeValue;
import software.amazon.awssdk.services.sqs.model.SendMessageRequest;

@Service
public class SqsNotificationService implements INotificationService {

    @Autowired
    private SqsClient sqs;

    @Value("${sqs.queue_url}")
    private String queueUrl;

    private Optional<String> actualQueueUrl = Optional.empty();

    @Override
    public void sendYo(String sourceUser, String targetUser) {
        if (actualQueueUrl.isEmpty()) {
            actualQueueUrl = Optional.of(sqs.getQueueUrl(builder -> builder.queueName(queueUrl)).queueUrl());
        }

        var model = new NotificationModel(sourceUser, targetUser);
        var gson = new Gson();
        var json = gson.toJson(model);

        var span = Span.current().getSpanContext();
        var traceId = span.getTraceId();
        var spanId = span.getSpanId();

        var attributes = Map.of(
            "ParentTraceId", MessageAttributeValue.builder().dataType("String").stringValue(traceId).build(),
            "ParentSpanId", MessageAttributeValue.builder().dataType("String").stringValue(spanId).build()
        );

        var sendMessageRequest = SendMessageRequest.builder()
            .messageBody(json)
            .messageAttributes(attributes)
            .queueUrl(actualQueueUrl.get())
            .build();

        sqs.sendMessage(sendMessageRequest);
    }

}
