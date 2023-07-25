package dev.kipters.yodemo.controller;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import dev.kipters.yodemo.service.INotificationService;
import io.opentelemetry.api.GlobalOpenTelemetry;
import io.opentelemetry.api.trace.Span;
import io.opentelemetry.api.trace.Tracer;

@RestController
@RequestMapping("/yo")
public class YoController {

    @Autowired
    private INotificationService notificationService;

    @PostMapping
    public ResponseEntity<?> sendYo(String targetUser) {
        var sourceUser = SecurityContextHolder.getContext().getAuthentication().getName();

        notificationService.sendYo(sourceUser, targetUser);
        return ResponseEntity.ok(targetUser);
    }
}
