package dev.kipters.yodemo.controller;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import dev.kipters.yodemo.service.INotificationService;

@RestController
@RequestMapping("/yo")
public class YoController {

    @Autowired
    private INotificationService notificationService;

    @PostMapping
    public ResponseEntity<?> sendYo(String targetUser) {
        notificationService.sendYo("ME", targetUser);
        return ResponseEntity.ok(targetUser);
    }
}
